using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication6.Data;
using WebApplication6.Models;
using WebApplication6.RequestDTO;


namespace WebApplication6.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        public AuthController(AppDbContext context, IConfiguration config, IUserService userService)
        {
            _context = context;
            _config = config;
            _userService = userService;
        }

        // 🔹 REGISTER
        [HttpPost("register")]
        public IActionResult Register([FromBody] WebApplication6.RequestDTO.RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest(new { message = "User with this email already exists" });

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Password = request.Password, // ⚠️ plaintext, hash უკეთესი
                Role = "User"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { user.Id, user.FirstName, user.LastName, user.Email });
        }


        // 🔹 LOGIN
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.Email == request.Email && u.Password == request.Password);

            if (user == null)
                return Unauthorized(new { message = "Email ან პაროლი არასწორია" });

            if (user.Email == "kikaliamerab9@gmail.com")
            {
                user.Role = "Admin";
                _context.SaveChanges();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = user.Role
            });
        }

        // 🔹 FORGOT PASSWORD
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest(new { message = "Email უნდა იყოს შევსებული" });

            var result = await _userService.SendPasswordResetEmailAsync(request.Email);

            if (!result)
                return NotFound(new { message = "მომხმარებელი არ მოიძებნა" });

            return Ok(new { message = "პაროლის აღდგენის ინსტრუქცია გამოგზავნილია ელ. ფოსტაზე" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] WebApplication6.RequestDTO.ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
                return BadRequest(new { message = "Token და NewPassword უნდა იყოს შევსებული" });

            var result = await _userService.ResetPasswordAsync(request.Token, request.NewPassword);

            if (!result)
                return BadRequest(new { message = "Token არასწორია ან ვადის გასული" });

            return Ok(new { message = "პაროლი წარმატებით განახლდა" });
        }

        // 🔹 ME (current user)
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return Ok(new { name, email, role });
        }
    }
}
