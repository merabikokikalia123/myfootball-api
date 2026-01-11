using System.Net;
using System.Net.Mail;

namespace WebApplication6.Helpers;

public static class EmailService
{
    public static void Send(string to, string subject, string body)
    {
        // Gmail SMTP
        using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential("lukanoniashvili@gmail.com", "zccn vhxt cltw sgwr") // App Password
        };

        var mail = new MailMessage
        {
            From = new MailAddress("lukanoniashvili@gmail.com"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(to);

        smtpClient.Send(mail);
    }
}
