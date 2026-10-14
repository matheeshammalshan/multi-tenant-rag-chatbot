using Application.DTOs;

namespace Application.Interfaces.Repositories
{
    public interface IChatHistoryRepository
    {
        Task AddAsync(int chatBotId, string userQuery, string systemQuery);
        Task<List<ChatHistoryResponseDto>> GetAsync(int chatBotId);
        Task AddConversationAsync(int chatBotId, Guid conversationId, string userQuery, string systemQuery);
        Task<List<ChatHistoryResponseDto>> GetPageAsync(int chatBotId, Guid? conversationId, int skip, int take);
    }
}
