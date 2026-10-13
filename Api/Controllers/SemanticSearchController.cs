using Application.Interfaces.Services;
using Api.Filters;
using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [RequiredApiKey]
    public class SemanticSearchController: ControllerBase
    {
        private readonly ISemanticSearchService _semanticSearchService;
    
        public SemanticSearchController(ISemanticSearchService semanticSearchService)
        {
            _semanticSearchService = semanticSearchService;
        }
    
        [HttpPost("askQuery")]
        public async Task<string> AskQueary(SematicSearchingDto semanticSearchingDto)
        {
        
            var apiKey=HttpContext.Items["ApiKey"]?.ToString();
        
            if (apiKey == null)
            {
                return "no api key found";
            }
        
            semanticSearchingDto.ApiKey = apiKey;
            return await _semanticSearchService.SearchEmbeddings(semanticSearchingDto);
        
        }
    }
}