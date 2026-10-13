using Application.Interfaces.Services;
using Application.DTOs;
using Application.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatBotController:ControllerBase
    {
        private readonly IChatBotService _chatBotService;

        public ChatBotController(IChatBotService chatBotService)
        {
            _chatBotService = chatBotService;
        }
    
        [HttpPost("create")]
        public async Task<IActionResult> CreateChatBot([FromBody]ChatBotDto chatBotDto)
        {
            await _chatBotService.CreateChatBot(chatBotDto);
            return Ok();
        }
    }
}