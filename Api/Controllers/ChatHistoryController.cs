using Application.Interfaces.Services;
using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatHistoryController
    {
        private readonly IChatHistoryService _chatHistoryService;
    
        public ChatHistoryController(IChatHistoryService chatHistoryService)
        {
            _chatHistoryService=chatHistoryService;
        
        }
    
        [HttpGet("{chatBotId}")]
        public async Task<List<ChatHistoryResponseDto>> GetChatHistory(int chatBotId)
        {
            return await _chatHistoryService.RetriveAllChatHistory(chatBotId);
        }
    }
}