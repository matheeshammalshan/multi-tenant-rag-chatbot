using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly IApplicationRepository _appDb;
        private readonly ILlmConfigService _configService;
        private readonly ILogger<ChatBotService> _logger;
        private readonly ICurrentUser _currentUser;
        public ChatBotService(IApplicationRepository appDb, ILlmConfigService configService, ILogger<ChatBotService> logger, ICurrentUser currentUser)
        {
            _appDb = appDb;
            _configService = configService;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<ChatBotEntity> CreateChatBot(ChatBotDto chatBotDto)
        {
            var chatBotEntity = new ChatBotEntity
            {
                Name = chatBotDto.Name,
                Description = chatBotDto.Description,
                OrganizationId = chatBotDto.OrganizationId,
                SystemPromt = chatBotDto.SystemPromt
            };
            chatBotEntity.CreatedByUserId = _currentUser.Id;
            using var transaction = await _appDb.BeginTransactionAsync();
            try
            {
                await _appDb.AddAsync(chatBotEntity);
                await _appDb.SaveChangesAsync();
                var result = await _configService.DefaultConfig(new LlmConfigurationsDto(chatBotEntity.Id, LlmModelsDto.GptOss20B, 0.5, 1024, 0.9));
                await transaction.CommitAsync();
                _logger.LogInformation($"Created chatbot with id {chatBotEntity.Id}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating chatbot entity");
                throw;
            }

            return chatBotEntity;
        }
    }
}
