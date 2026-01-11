using Microsoft.EntityFrameworkCore;
using WebApplication6.Models;

namespace WebApplication6.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PlayerProfile> PlayerProfiles { get; set; }
    public DbSet<User> Users { get; set; }

    public DbSet<News> News { get; set; }
}

