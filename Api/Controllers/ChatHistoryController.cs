using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("ChatHistory")]
    public class ChatHistoryController
    {
        private readonly IChatHistoryRepository repository;
        private readonly IResourceAccessService access;
        public ChatHistoryController(IChatHistoryRepository repository, IResourceAccessService access)
        {
            this.repository = repository;
            this.access = access;
        }

        [HttpGet("{chatBotId:int}")]
        public async Task<List<ChatHistoryResponseDto>> Get(int chatBotId, [FromQuery] Guid? conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            await access.RequireChatbotAsync(chatBotId);
            return await repository.GetPageAsync(chatBotId, conversationId, Math.Max(0, skip), Math.Clamp(take, 1, 200));
        }
    }
}
