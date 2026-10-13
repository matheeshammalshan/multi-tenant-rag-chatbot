using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ChatBotService: IChatBotService
    {
        private readonly IMapper _mapper;
        private readonly IApplicationRepository _appDb;
        private readonly ILlmConfigService _configService;
        private readonly ILogger<ChatBotService> _logger;

        public ChatBotService(IMapper mapper, IApplicationRepository appDb, ILlmConfigService configService, 
            ILogger<ChatBotService> logger)
        {
            _mapper = mapper;
            _appDb = appDb;
            _configService = configService;
            _logger = logger;
        }

        public async Task<ChatBotEntity> CreateChatBot(ChatBotDto chatBotDto)
        {
            var chatBotEntity = _mapper.Map<ChatBotDto,ChatBotEntity>(chatBotDto);
        
            using var transaction = await _appDb.BeginTransactionAsync();

            try
            {
                await _appDb.AddAsync(chatBotEntity);
                await _appDb.SaveChangesAsync();

                var result=await _configService.
                    DefaultConfig(new LlmConfigurationsDto(
                        chatBotEntity.Id,
                        LlmModelsDto.KimiK2,
                        1,
                        45,
                        1
                        ));
            
                await transaction.CommitAsync();
                _logger.LogInformation($"Created chatbot with id {chatBotEntity.Id}");

            }
            catch (Exception e)
            {
                _logger.LogError(e,"Error creating chatbot entity");
                throw;
            }
        
            return chatBotEntity;
        }
    
    
    
    }
}