using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;

namespace Application.Services
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly IChatHistoryRepository historyRepository;
        public ChatHistoryService(IChatHistoryRepository historyRepository)
        {
            this.historyRepository = historyRepository;
        }

        public Task<List<ChatHistoryResponseDto>> RetriveAllChatHistory(int chatBotId)
        {
            return historyRepository.GetAsync(chatBotId);
        }
    }
}
