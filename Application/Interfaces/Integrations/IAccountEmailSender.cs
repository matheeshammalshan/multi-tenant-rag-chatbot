namespace Application.Interfaces.Integrations
{
    public interface IAccountEmailSender
    {
        Task SendPasswordResetAsync(string email, string token);
    }
}
