using System.Net;
using System.Net.Mail;

namespace AutoCurriculum.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var mail = _config["EmailSettings:Email"];
        var pw = _config["EmailSettings:Password"];

        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(mail, pw)
        };

        var mailMessage = new MailMessage(from: mail, to: email, subject, message)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(mailMessage);
    }
}