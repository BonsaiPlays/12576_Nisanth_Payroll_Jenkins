using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace PayrollApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var msg = BuildMessage(to, subject, htmlBody, isHtml: true);
            await Send(msg);
        }

        public async Task SendPlainAsync(string to, string subject, string textBody)
        {
            var msg = BuildMessage(to, subject, textBody, isHtml: false);
            await Send(msg);
        }

        public async Task SendTemplatedAsync(
            string to,
            string subject,
            string content,
            string? actionText = null,
            string? actionUrl = null
        )
        {
            var htmlBody = EmailTemplate.Build(subject, content, actionText, actionUrl);
            await SendAsync(to, subject, htmlBody);
        }

        private MimeMessage BuildMessage(string to, string subject, string body, bool isHtml)
        {
            var smtp = _config.GetSection("Smtp");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp["FromName"], smtp["FromEmail"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;
            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        private async Task Send(MimeMessage message)
        {
            var smtp = _config.GetSection("Smtp");
            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["Host"],
                int.Parse(smtp["Port"]!),
                MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable
            );
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
