using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using WebApplication6.Data;
using WebApplication6.Models;

var builder = WebApplication.CreateBuilder(args);

static string NormalizePostgresConnectionString(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return raw;

    raw = raw.Trim();

    // Strip surrounding quotes if present (common when copy-pasting)
    if (raw.Length >= 2 && ((raw.StartsWith('"') && raw.EndsWith('"')) || (raw.StartsWith('\'') && raw.EndsWith('\''))))
        raw = raw[1..^1].Trim();

    // Convert URL style (postgresql://user:pass@host:port/db?sslmode=require) to Npgsql key/value style
    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);

        var username = string.Empty;
        var password = string.Empty;
        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
                password = Uri.UnescapeDataString(parts[1]);
        }

        var database = uri.AbsolutePath.TrimStart('/');

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
        };

        // Prefer IPv4 when the driver supports it (helps in environments without IPv6 routes)
        var preferIpv4Prop = typeof(NpgsqlConnectionStringBuilder).GetProperty("PreferIPv4");
        if (preferIpv4Prop?.CanWrite == true && preferIpv4Prop.PropertyType == typeof(bool))
            preferIpv4Prop.SetValue(csb, true);

        // Parse a couple of common query params (ignore unknowns like channel_binding)
        var query = uri.Query;
        if (!string.IsNullOrWhiteSpace(query))
        {
            if (query.StartsWith("?", StringComparison.Ordinal))
                query = query[1..];

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]).Trim();
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]).Trim() : string.Empty;

                if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Equals("disable", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.Disable;
                    else if (value.Equals("prefer", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.Prefer;
                    else if (value.Equals("allow", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.Allow;
                    else if (value.Equals("require", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.Require;
                    else if (value.Equals("verify-ca", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.VerifyCA;
                    else if (value.Equals("verify-full", StringComparison.OrdinalIgnoreCase)) csb.SslMode = SslMode.VerifyFull;
                }
            }
        }

        return csb.ConnectionString;
    }

    return raw;
}

// ✅ Hosting port (PaaS)
// Some hosts provide a PORT env var. Bind Kestrel to it if present.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ✅ Database
// Default to SQLite for local dev (no LocalDB install required).
var dbProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=webapplication6-dev.db";
var postgresConn = builder.Configuration.GetConnectionString("PostgresConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConn);
    }
    else if (string.Equals(dbProvider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(dbProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(dbProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(postgresConn))
            throw new InvalidOperationException("Missing configuration: ConnectionStrings:PostgresConnection");

        options.UseNpgsql(NormalizePostgresConnectionString(postgresConn));
    }
    else
    {
        options.UseSqlite(sqliteConn);
    }
});

// ✅ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing configuration: Jwt:Key");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key is too short for HS256. Use at least 32 bytes.");

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateLifetime = true
    };
});
builder.Services.AddScoped<IUserService, UserService>();

// ✅ Authorization
builder.Services.AddAuthorization();

// ✅ Controllers
builder.Services.AddControllers();

// ✅ CORS
// Configure allowed origins for hosted frontend.
// - Dev: allows http://localhost:4200
// - Prod: set env var CORS_ALLOWED_ORIGINS="https://your-frontend.com,https://www.your-frontend.com"
//   or appsettings: Cors:AllowedOrigins
var allowedOrigins = new List<string>();

var envAllowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"];
if (!string.IsNullOrWhiteSpace(envAllowedOrigins))
{
    allowedOrigins.AddRange(
        envAllowedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    );
}
else
{
    var configAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (configAllowedOrigins is { Length: > 0 })
        allowedOrigins.AddRange(configAllowedOrigins);
}

if (builder.Environment.IsDevelopment())
{
    allowedOrigins.Add("http://localhost:4200");
}

allowedOrigins = allowedOrigins
    .Select(o => o.Trim())
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowedOrigins.Count == 0)
        {
            // Fallback to permissive CORS to prevent accidental lockouts.
            // Tighten this in production by setting CORS_ALLOWED_ORIGINS.
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.ToArray());
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ✅ Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseCors("AppCors");

app.UseAuthentication();
app.UseAuthorization();

// ✅ Map controllers
app.MapControllers();

// ✅ Initialize DB
// NOTE: The existing EF migrations in this repo were generated for SQL Server and include SQL Server-specific
// column types like nvarchar(max). Applying them to SQLite will fail ("near 'max': syntax error").
// For SQLite, create the schema directly.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine($"[DB] Config Database:Provider = '{dbProvider}'. EF provider = '{db.Database.ProviderName}'.");

    if (string.Equals(dbProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Users.Any(u => u.Email == "kikaliamerab9@gmail.com"))
    {
        db.Users.Add(new User
        {
            FirstName = "Adminiso",
            LastName = "bradada",
            Email = "kikaliamerab9@gmail.com",
            Password = "dosta!£s", // ⚠️ plaintext
            Role = "Admin",
               Phone = "" // ან "N/A"
        });

        db.SaveChanges();
    }

    // Dev seed content so the UI isn't empty
    if (!db.News.Any())
    {
        db.News.AddRange(
            new News { Title = "Football: Welcome", Content = "Sample football news from backend.", Category = "football", CreatedAt = DateTime.UtcNow },
            new News { Title = "Judo: Welcome", Content = "Sample judo news from backend.", Category = "judo", CreatedAt = DateTime.UtcNow },
            new News { Title = "Basketball: Welcome", Content = "Sample basketball news from backend.", Category = "basketball", CreatedAt = DateTime.UtcNow },
            new News { Title = "MMA: Welcome", Content = "Sample MMA news from backend.", Category = "mma", CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }

    if (!db.PlayerProfiles.Any())
    {
        db.PlayerProfiles.AddRange(
            new PlayerProfile
            {
                Name = "Demo Footballer",
                Age = 22,
                Sport = "Football",
                Position = "ST",
                Height = 180,
                Country = "GE",
                PhotoUrl = "https://via.placeholder.com/300",
                VideoUrl = "https://www.youtube.com/embed/"
            },
            new PlayerProfile
            {
                Name = "Demo Judoka",
                Age = 25,
                Sport = "Judo",
                WeightCategory = "-73",
                Belt = "Black",
                Country = "GE",
                PhotoUrl = "https://via.placeholder.com/300",
                VideoUrl = "https://www.youtube.com/embed/"
            }
        );
        db.SaveChanges();
    }
}
// ✅ Run
app.Run();
