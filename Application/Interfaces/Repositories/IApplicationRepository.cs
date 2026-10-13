using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IApplicationRepository
    {
        Task<ChatBotEntity?> FindChatBotAsync(int id);
        Task<LlmConfigurationEntity?> FindConfigurationAsync(int chatBotId);
        Task AddAsync<T>(T entity) where T : class;
        void Update<T>(T entity) where T : class;
        Task<int> SaveChangesAsync();
        Task<IApplicationTransaction> BeginTransactionAsync();
    }

    public interface IApplicationTransaction : IDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}