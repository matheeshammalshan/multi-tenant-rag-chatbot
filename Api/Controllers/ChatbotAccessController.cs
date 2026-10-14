using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController, Route("ChatbotAccess"), AllowAnonymous, EnableRateLimiting("Chat")]
    public class ChatbotAccessController : ControllerBase
    {
        private readonly IApiKeyService keys;
        public ChatbotAccessController(IApiKeyService keys)
        {
            this.keys = keys;
        }

        [HttpGet]
        public Task<ChatbotAccessDto> Get([FromHeader(Name = "x-api-key")] string? key)
        {
            return keys.ResolveAsync(key ?? "");
        }
    }
}
