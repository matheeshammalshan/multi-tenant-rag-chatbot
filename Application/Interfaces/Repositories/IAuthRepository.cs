using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IAuthRepository
    {
        Task<UserAccount?> FindUserByEmailAsync(string email);
        Task<UserAccount?> FindUserAsync(Guid id);
        Task<UserSession?> FindSessionAsync(Guid id);
        Task<UserSession?> FindRefreshSessionAsync(string hash);
        Task<PasswordResetToken?> FindResetAsync(string hash);
        Task AddAsync<T>(T entity)
            where T : class;
        Task SaveAsync();
        Task<bool> RotateSessionAsync(Guid id, string previousHash, string newHash, string csrfHash);
        Task<bool> ConsumeResetAsync(Guid id);
        Task RevokeSessionsAsync(Guid userId);
    }
}
