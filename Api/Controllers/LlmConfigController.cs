using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Exceptions;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("LlmConfig")]
    public class LlmConfigController : ControllerBase
    {
        private readonly ILlmConfigService service;
        private readonly IResourceAccessService access;
        private readonly IApplicationRepository repository;
        public LlmConfigController(ILlmConfigService service, IResourceAccessService access, IApplicationRepository repository)
        {
            this.service = service;
            this.access = access;
            this.repository = repository;
        }

        [HttpGet("{chatBotId:int}")]
        public async Task<LlmConfigurationsDto> Get(int chatBotId)
        {
            await access.RequireChatbotAsync(chatBotId);
            var config = await repository.FindConfigurationAsync(chatBotId) ?? throw ApiException.NotFound("Configuration");
            return new LlmConfigurationsDto(config.ChatBotId, config.ModelName, config.Temperature, config.MaxToken, config.TopP);
        }

        [HttpPut("update")]
        public async Task<LlmConfigurationsDto> Update(LlmConfigurationsDto request)
        {
            await access.RequireChatbotAsync(request.ChatBotId);
            if (request.ModelName != LlmModelsDto.GptOss20B && request.ModelName != LlmModelsDto.GptOss120B)
            {
                throw new ApiException(400, "This model has been retired by Groq. Choose an available model from /Models.");
            }

            await service.CreateConfig(request);
            return await Get(request.ChatBotId);
        }
    }
}
