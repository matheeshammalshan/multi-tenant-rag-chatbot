using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Infrastructure.Repositories
{
    public sealed class EfApplicationRepository(AppDb db) : IApplicationRepository
    {
        public async Task<ChatBotEntity?> FindChatBotAsync(int id) => await db.ChatBots.FindAsync(id);
        public Task<LlmConfigurationEntity?> FindConfigurationAsync(int chatBotId) => db.LlmConfig.FirstOrDefaultAsync(c => c.ChatBotId == chatBotId);
        public async Task AddAsync<T>(T entity) where T : class => await db.AddAsync(entity);
        public void Update<T>(T entity) where T : class => db.Update(entity);
        public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
        public async Task<IApplicationTransaction> BeginTransactionAsync() => new EfTransaction(await db.Database.BeginTransactionAsync());
        private sealed class EfTransaction(IDbContextTransaction transaction) : IApplicationTransaction
        {
            public Task CommitAsync() => transaction.CommitAsync();
            public Task RollbackAsync() => transaction.RollbackAsync();
            public void Dispose() => transaction.Dispose();
        }
    }
}