 using System.Net;
 using System.Net.Mail;
 using System.Threading.Tasks;
namespace DATNN.Service
{
    public class EmailService : IEmailService
    {
        public async Task SendRegisterConfirmation(string toEmail, string ma)
        {
            using (var client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.Credentials = new NetworkCredential("dathtph45372@fpt.edu.vn", "jzfk t spc x lgc ymdo");
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("dathtph45372@fpt.edu.vn"),
                    Subject = "Mã Xác Nhận Đăng Ký Tài Khoản",
                    Body = $"<p>Mã xác nhận của bạn là: <strong>{ma}</strong></p>",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("dathtph45372@fpt.edu.vn", "jzfk t spc x lgc ymdo"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("dathtph45372@fpt.edu.vn"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
