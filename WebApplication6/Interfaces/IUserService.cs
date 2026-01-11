using WebApplication6.Data;

public interface IUserService
{
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return false;

        // Token + ვადა
        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiration = DateTime.Now.AddHours(1);

        await _context.SaveChangesAsync();

        var resetLink = $"http://localhost:4200/reset-password?token={user.PasswordResetToken}";
        Console.WriteLine($"Password reset link for {email}: {resetLink}");

        // TODO: გაგზავნე Email SMTP / SendGrid
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = _context.Users.FirstOrDefault(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiration > DateTime.Now
        );

        if (user == null) return false;

        user.Password = newPassword; // ⚠️ hash უკეთესი
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiration = null;

        await _context.SaveChangesAsync();
        return true;
    }
}
