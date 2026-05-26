using System.Threading.Tasks;

namespace BACK.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string body, byte[]? attachmentBytes = null, string? attachmentName = null);
    }
}
