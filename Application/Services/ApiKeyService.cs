using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ApiKeyService: IApiKeyService
    {
        private readonly IApplicationRepository _appDb;
    
        public ApiKeyService(IApplicationRepository appDb)
        {
            _appDb=appDb;
        
        }
        public async Task<string> GenerateApiKey(ApiKeyDto dto)
        {
            var chatBotEntity = await _appDb.FindChatBotAsync(dto.ChatBotId);
            if (chatBotEntity == null)
            {
                return "not chat bot found";
            }
            string apiKey = Guid.NewGuid().ToString();
            chatBotEntity.ApiKey = apiKey;
        
            _appDb.Update(chatBotEntity);
            await _appDb.SaveChangesAsync();
        
            return apiKey;

        }
    }
}