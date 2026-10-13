using Application.Interfaces.Services;
using Api.Filters;
using Application.DTOs;
using Application.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController
    {
        private readonly IApiKeyService _apiKeyService;
    
        public ApiController(IApiKeyService apiKeyService)
        {
            _apiKeyService = apiKeyService;
        
        }
        
        [HttpGet("create-apikey")]
        public async Task<string> GenerateApiKey([FromBody]ApiKeyDto dto)
        {
            return await _apiKeyService.GenerateApiKey(dto);
        }
    }
}