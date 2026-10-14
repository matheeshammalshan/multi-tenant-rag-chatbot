using Application.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Models")]
    public class ModelsController : ControllerBase
    {
        [HttpGet]
        public ModelDto[] Get()
        {
            return new ModelDto[]
            {
                new ModelDto(LlmModelsDto.GptOss20B, "GPT-OSS 20B", "openai/gpt-oss-20b", true, null),
                new ModelDto(LlmModelsDto.GptOss120B, "GPT-OSS 120B", "openai/gpt-oss-120b", true, null),
                new ModelDto(LlmModelsDto.KimiK2, "Kimi K2", "moonshotai/kimi-k2-instruct-0905", false, "Retired by Groq; select GPT-OSS."),
                new ModelDto(LlmModelsDto.Llama4Scout, "Llama 4 Scout", "meta-llama/llama-4-scout-17b-16e-instruct", false, "Retired by Groq; select GPT-OSS.")
            };
        }
    }
}
