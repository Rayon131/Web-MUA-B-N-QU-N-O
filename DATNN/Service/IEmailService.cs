namespace DATNN.Service
{
    public interface IEmailService
    {
        Task SendRegisterConfirmation(string toEmail, string ma);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
