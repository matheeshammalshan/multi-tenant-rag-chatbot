using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers
{
    [ApiController, Route("ChatBot"), Route("ChatBots")]
    public class ChatBotController : ControllerBase
    {
        private readonly IChatBotService service;
        private readonly IConsoleRepository console;
        private readonly ICurrentUser user;
        private readonly IResourceAccessService access;
        private readonly IAuthRepository accounts;
        public ChatBotController(IChatBotService service, IConsoleRepository console, ICurrentUser user, IResourceAccessService access, IAuthRepository accounts)
        {
            this.service = service;
            this.console = console;
            this.user = user;
            this.access = access;
            this.accounts = accounts;
        }

        [HttpGet]
        public async Task<List<ChatbotSummaryDto>> List([FromQuery] int? organizationId, [FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            if (organizationId is int id)
            {
                await access.RequireOrganizationAsync(id);
            }

            return await console.ChatbotsAsync(user.IsOwner ? organizationId : user.OrganizationId ?? -1, user.IsOwner ? null : user.Id ?? Guid.Empty, Math.Max(0, skip), Math.Clamp(take, 1, 200));
        }

        [HttpGet("{id:int}")]
        public async Task<ChatbotSummaryDto> Get(int id)
        {
            var bot = await access.RequireChatbotAsync(id);
            var bots = await console.ChatbotsAsync(bot.OrganizationId, user.IsOwner ? null : user.Id, 0, int.MaxValue);
            return bots.First(x => x.Id == id);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(ChatBotDto request)
        {
            await access.RequireOrganizationAsync(request.OrganizationId);
            var bot = await service.CreateChatBot(request);
            return Created($"/ChatBots/{bot.Id}", await Get(bot.Id));
        }

        [HttpPut("{id:int}")]
        public async Task<ChatbotSummaryDto> Update(int id, ChatbotUpdateRequest request)
        {
            var bot = await access.RequireChatbotAsync(id);
            bot.Name = request.Name.Trim();
            bot.Description = request.Description;
            bot.SystemPromt = request.SystemPrompt;
            await console.SaveAsync();
            return await Get(id);
        }

        [HttpPut("{id:int}/owner")]
        public async Task<IActionResult> AssignOwner(int id, [FromBody] Guid userId)
        {
            access.RequireOwner();
            var bot = await access.RequireChatbotAsync(id);
            var account = await accounts.FindUserAsync(userId) ?? throw ApiException.NotFound("User");
            if (!account.IsActive || account.OrganizationId != bot.OrganizationId)
            {
                throw new ApiException(400, "Assign an active user from the chatbot's organization.");
            }

            bot.CreatedByUserId = account.Id;
            await console.SaveAsync();
            return NoContent();
        }
    }

    public class ChatbotUpdateRequest
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = "";

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? SystemPrompt { get; set; }
    }
}
