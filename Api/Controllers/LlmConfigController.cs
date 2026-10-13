using Application.Interfaces.Services;
using Application.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LlmConfigController:ControllerBase
    {
        private readonly ILlmConfigService _configService;

        public LlmConfigController(ILlmConfigService configService)
        {
            _configService = configService;
        }
    
        [HttpPut("update")]
        public async Task<IActionResult> CreateConfig([FromBody] LlmConfigurationsDto configDto)
        {
            await _configService.CreateConfig(configDto);
            return Ok();
        }
    }
}