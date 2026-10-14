using Application.DTOs;

namespace Application.Interfaces.Services
{
    public interface IChatHistoryService
    {
        Task<List<ChatHistoryResponseDto>> RetriveAllChatHistory(int chatBotId);
    }
}
