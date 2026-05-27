using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BACK.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string body, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = body
                };

                if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrEmpty(attachmentName))
                {
                    builder.Attachments.Add(attachmentName, attachmentBytes);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Allow self-signed certs for testing and ease of SMTP
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Determine connection security
                SecureSocketOptions socketOption = _emailSettings.EnableSsl 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.None;

                if (_emailSettings.SmtpPort == 465)
                {
                    socketOption = SecureSocketOptions.SslOnConnect;
                }

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, socketOption);

                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_emailSettings.SenderEmail) && !string.IsNullOrEmpty(_emailSettings.SenderPassword))
                {
                    await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âŒ Error in EmailService.SendEmailAsync: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw new Exception($"Error al enviar el correo: {ex.Message}", ex);
            }
        }
    }
}
