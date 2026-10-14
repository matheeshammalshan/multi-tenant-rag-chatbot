using Application.DTOs.Accounts;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthSessionResult> RegisterAsync(RegisterRequest request);
        Task<AuthSessionResult> LoginAsync(LoginRequest request);
        Task<AuthSessionResult> RefreshAsync(string refreshToken, string csrfToken);
        Task LogoutAsync(Guid? sessionId, string? refreshToken);
        Task<UserDto> GetUserAsync(Guid userId);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task BootstrapOwnerAsync(string email, string password, string name);
    }
}
