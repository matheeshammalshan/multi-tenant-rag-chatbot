using System.Net;
using System.Net.Mail;
using Application.Interfaces.Integrations;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Auth
{
    public class SmtpAccountEmailSender : IAccountEmailSender
    {
        private readonly IConfiguration configuration;
        public SmtpAccountEmailSender(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task SendPasswordResetAsync(string email, string token)
        {
            var host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Configure Smtp:Host for password reset email.");
            using var client = new SmtpClient(host, configuration.GetValue("Smtp:Port", 1025))
            {
                EnableSsl = configuration.GetValue("Smtp:UseSsl", false),
                Timeout = 15000
            };
            if (!string.IsNullOrWhiteSpace(configuration["Smtp:Username"]))
            {
                client.Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]);
            }

            var frontend = configuration["Frontend:PasswordResetUrl"] ?? "http://localhost:5173/reset-password";
            using var message = new MailMessage(configuration["Smtp:From"] ?? "noreply@chatbot.local", email)
            {
                Subject = "Reset your chatbot console password",
                Body = $"Reset your password within 30 minutes: {frontend}?token={Uri.EscapeDataString(token)}"};
            await client.SendMailAsync(message);
        }
    }
}
