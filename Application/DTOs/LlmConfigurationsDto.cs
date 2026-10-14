using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class LlmConfigurationsDto
    {
        [Range(1, int.MaxValue)]
        public int ChatBotId { get; set; }
        public LlmModelsDto ModelName { get; set; }

        [Range(0, 2)]
        public double Temperature { get; set; }

        [Range(128, 8192)]
        public int MaxToken { get; set; }

        [Range(0.01, 1)]
        public double TopP { get; set; }
        public string? SystemPrompt { get; set; }

        public LlmConfigurationsDto(int chatBotId, LlmModelsDto modelName, double temperature, int maxToken, double topP)
        {
            ChatBotId = chatBotId;
            ModelName = modelName;
            Temperature = temperature;
            MaxToken = maxToken;
            TopP = topP;
        }
    }
}
