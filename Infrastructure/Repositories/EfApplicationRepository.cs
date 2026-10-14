using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Application.Security;

namespace Infrastructure.Repositories
{
    public sealed class EfApplicationRepository : IApplicationRepository
    {
        private readonly AppDb db;
        public EfApplicationRepository(AppDb db)
        {
            this.db = db;
        }

        public async Task<ChatBotEntity?> FindChatBotAsync(int id)
        {
            return await db.ChatBots.FindAsync(id);
        }

        public Task<LlmConfigurationEntity?> FindConfigurationAsync(int chatBotId)
        {
            return db.LlmConfig.FirstOrDefaultAsync(c => c.ChatBotId == chatBotId);
        }

        public Task<ChatBotEntity?> FindChatBotByApiKeyAsync(string key)
        {
            var hash = SecretTokens.Hash(key);
            return db.ChatBots.FirstOrDefaultAsync(x => x.ApiKeyHash == hash || (x.ApiKeyHash == null && x.ApiKey == key));
        }

        public Task<DocumentEntity?> FindDocumentAsync(int id)
        {
            return db.Document.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<int[]> ReadyDocumentIdsAsync(int chatbotId)
        {
            return db.Document.Where(x => x.ChatBotId == chatbotId && x.Status == "Proceed").Select(x => x.Id).ToArrayAsync();
        }

        public async Task RecordQueryAsync(int chatbotId)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO `Analysis` (`ChatBotId`, `TotalQueries`, `NumberOfDocuments`, `TotalChunks`) VALUES ({chatbotId}, 1, 0, 0) ON DUPLICATE KEY UPDATE `TotalQueries` = `TotalQueries` + 1");
        }

        public async Task AddAsync<T>(T entity)
            where T : class
        {
            await db.AddAsync(entity);
        }

        public void Update<T>(T entity)
            where T : class
        {
            db.Update(entity);
        }

        public Task<int> SaveChangesAsync()
        {
            return db.SaveChangesAsync();
        }

        public async Task<IApplicationTransaction> BeginTransactionAsync()
        {
            return new EfTransaction(await db.Database.BeginTransactionAsync());
        }

        private sealed class EfTransaction : IApplicationTransaction
        {
            private readonly IDbContextTransaction transaction;
            public EfTransaction(IDbContextTransaction transaction)
            {
                this.transaction = transaction;
            }

            public Task CommitAsync()
            {
                return transaction.CommitAsync();
            }

            public Task RollbackAsync()
            {
                return transaction.RollbackAsync();
            }

            public void Dispose()
            {
                transaction.Dispose();
            }
        }
    }
}
