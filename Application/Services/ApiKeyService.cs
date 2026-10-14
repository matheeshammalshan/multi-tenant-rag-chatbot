using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Security;

namespace Application.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IApplicationRepository _appDb;
        public ApiKeyService(IApplicationRepository appDb)
        {
            _appDb = appDb;
        }

        public async Task<string> GenerateApiKey(ApiKeyDto dto)
        {
            var chatBotEntity = await _appDb.FindChatBotAsync(dto.ChatBotId);
            if (chatBotEntity == null)
            {
                throw ApiException.NotFound("Chatbot");
            }

            string apiKey = "cb_" + SecretTokens.Generate();
            chatBotEntity.ApiKey = null;
            chatBotEntity.ApiKeyHash = SecretTokens.Hash(apiKey);
            chatBotEntity.ApiKeyPrefix = apiKey.Substring(0, 11);
            _appDb.Update(chatBotEntity);
            await _appDb.SaveChangesAsync();
            return apiKey;
        }

        public async Task<ChatbotAccessDto> ResolveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
            {
                throw new ApiException(401, "Invalid chatbot API key.");
            }

            var bot = await _appDb.FindChatBotByApiKeyAsync(key);
            if (bot == null || !IsValid(bot, key))
            {
                throw new ApiException(401, "Invalid chatbot API key.");
            }

            return new ChatbotAccessDto(bot.Id, bot.Name, bot.Description);
        }

        public static bool IsValid(ChatBotEntity bot, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            if (bot.ApiKeyHash != null)
            {
                return SecretTokens.Matches(key, bot.ApiKeyHash);
            }
            if (bot.ApiKey != null)
            {
                string hash = SecretTokens.Hash(bot.ApiKey);
                return SecretTokens.Matches(key, hash);
            }
            return false;
        }
    }
}
