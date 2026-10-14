using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Api")]
    public class ApiController : ControllerBase
    {
        private readonly IApiKeyService service;
        private readonly IResourceAccessService access;
        public ApiController(IApiKeyService service, IResourceAccessService access)
        {
            this.service = service;
            this.access = access;
        }

        [HttpPost("create-apikey")]
        public async Task<ApiKeyResult> GenerateApiKey(ApiKeyDto request)
        {
            await access.RequireChatbotAsync(request.ChatBotId);
            var key = await service.GenerateApiKey(request);
            return new ApiKeyResult(request.ChatBotId, key, key.Substring(0, 11));
        }

        [HttpPost("/ChatBots/{id:int}/api-key")]
        public Task<ApiKeyResult> Rotate(int id)
        {
            return GenerateApiKey(new ApiKeyDto { ChatBotId = id });
        }
    }
}
