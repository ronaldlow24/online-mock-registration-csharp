using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

namespace OnlineMockRegistration.Helper
{
    public class Utility 
    {
        public const string QueueName = "MockNameEmail";

        //validate email using regex
        public static bool ValidateEmail(string email)
        {
            string pattern = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
            return Regex.IsMatch(email, pattern);
        }

        public static async Task SendMailAsync(string subject, string message, IEnumerable<string> recipients, CancellationToken cancellationToken = default)
        {
            try
            {
                using var smtpClient = new SmtpClient(ApplicationConfiguration.StaticCurrent.EmailSmtpServer)
                {
                    Port = Convert.ToInt32(ApplicationConfiguration.StaticCurrent.EmailPort),
                    Credentials = new NetworkCredential(ApplicationConfiguration.StaticCurrent.EmailUserName, ApplicationConfiguration.StaticCurrent.EmailPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(ApplicationConfiguration.StaticCurrent.EmailFrom),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true,
                };

                foreach (var item in recipients)
                {
                    mailMessage.To.Add(item);
                }

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
