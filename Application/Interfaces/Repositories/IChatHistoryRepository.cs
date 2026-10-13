using Application.DTOs;
namespace Application.Interfaces.Repositories
{
    public interface IChatHistoryRepository
    {
        Task AddAsync(int chatBotId, string userQuery, string systemQuery);
        Task<List<ChatHistoryResponseDto>> GetAsync(int chatBotId);
    }
}