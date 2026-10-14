using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDb db;
        public AuthRepository(AppDb db)
        {
            this.db = db;
        }

        public Task<UserAccount?> FindUserByEmailAsync(string email)
        {
            return db.Users.Include(x => x.Organization).FirstOrDefaultAsync(x => x.Email == email);
        }

        public Task<UserAccount?> FindUserAsync(Guid id)
        {
            return db.Users.Include(x => x.Organization).FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<UserSession?> FindSessionAsync(Guid id)
        {
            return db.Sessions.Include(x => x.User).ThenInclude(x => x.Organization).FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<UserSession?> FindRefreshSessionAsync(string hash)
        {
            return db.Sessions.Include(x => x.User).ThenInclude(x => x.Organization).FirstOrDefaultAsync(x => x.RefreshTokenHash == hash);
        }

        public Task<PasswordResetToken?> FindResetAsync(string hash)
        {
            return db.PasswordResetTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash);
        }

        public async Task AddAsync<T>(T entity)
            where T : class
        {
            await db.AddAsync(entity);
        }

        public async Task SaveAsync()
        {
            await db.SaveChangesAsync();
        }

        public async Task<bool> RotateSessionAsync(Guid id, string previousHash, string newHash, string csrfHash)
        {
            return await db.Sessions.Where(x => x.Id == id && x.RefreshTokenHash == previousHash && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).ExecuteUpdateAsync(s => s.SetProperty(x => x.RefreshTokenHash, newHash).SetProperty(x => x.CsrfTokenHash, csrfHash)) == 1;
        }

        public async Task<bool> ConsumeResetAsync(Guid id)
        {
            return await db.PasswordResetTokens.Where(x => x.Id == id && x.UsedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow)) == 1;
        }

        public async Task RevokeSessionsAsync(Guid userId)
        {
            await db.Sessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow));
        }
    }
}
