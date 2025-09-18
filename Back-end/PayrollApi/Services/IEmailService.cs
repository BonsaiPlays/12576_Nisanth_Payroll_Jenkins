using System.Threading.Tasks;

namespace PayrollApi.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
        Task SendPlainAsync(string to, string subject, string textBody);
        Task SendTemplatedAsync(
            string to,
            string subject,
            string content,
            string? actionText = null,
            string? actionUrl = null
        );
    }
}
