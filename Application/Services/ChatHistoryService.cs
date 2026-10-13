using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
namespace Application.Services
{
    public class ChatHistoryService(IChatHistoryRepository historyRepository) : IChatHistoryService
    {
        public Task<List<ChatHistoryResponseDto>> RetriveAllChatHistory(int chatBotId) => historyRepository.GetAsync(chatBotId);
    }
}