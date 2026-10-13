using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Threading.Tasks;
using AutoMapper;


namespace Application.Services
{
    public class LlmConfigService: ILlmConfigService
    {
        private readonly IMapper _mapper;
        private readonly IApplicationRepository _appDb;

        public LlmConfigService(IMapper mapper, IApplicationRepository appDb)
        {
            _mapper = mapper;
            _appDb = appDb;
        }

        public async Task<LlmConfigurationEntity> CreateConfig(LlmConfigurationsDto configDto)
        {

            var config = await _appDb.FindConfigurationAsync(configDto.ChatBotId);

            if (config == null)
            {
                throw new Exception("Config not found");
            }

            var configurationEntity = _mapper.Map(configDto, config);
        
            _appDb.Update(configurationEntity);
            await _appDb.SaveChangesAsync();
            return _mapper.Map<LlmConfigurationEntity>(config);
        }
    
        public async Task<LlmConfigurationEntity> DefaultConfig(LlmConfigurationsDto configDto)
        {
            var configEntity = _mapper.Map<LlmConfigurationsDto, LlmConfigurationEntity>(configDto);
            await _appDb.AddAsync(configEntity);
            await _appDb.SaveChangesAsync();
        
            return configEntity;
        
        } 
    }
}