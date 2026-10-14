using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LlmConfigService : ILlmConfigService
    {
        private readonly IApplicationRepository _appDb;
        public LlmConfigService(IApplicationRepository appDb)
        {
            _appDb = appDb;
        }

        public async Task<LlmConfigurationEntity> CreateConfig(LlmConfigurationsDto configDto)
        {
            var config = await _appDb.FindConfigurationAsync(configDto.ChatBotId);
            if (config == null)
            {
                throw Application.Exceptions.ApiException.NotFound("Model configuration");
            }

            config.ModelName = configDto.ModelName;
            config.Temperature = configDto.Temperature;
            config.MaxToken = configDto.MaxToken;
            config.TopP = configDto.TopP;
            _appDb.Update(config);
            await _appDb.SaveChangesAsync();
            return config;
        }

        public async Task<LlmConfigurationEntity> DefaultConfig(LlmConfigurationsDto configDto)
        {
            var configEntity = new LlmConfigurationEntity
            {
                ChatBotId = configDto.ChatBotId,
                ModelName = configDto.ModelName,
                Temperature = configDto.Temperature,
                MaxToken = configDto.MaxToken,
                TopP = configDto.TopP
            };
            await _appDb.AddAsync(configEntity);
            await _appDb.SaveChangesAsync();
            return configEntity;
        }
    }
}
