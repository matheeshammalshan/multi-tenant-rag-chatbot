using Domain.Enums;

namespace Application.Interfaces.Integrations
{
    public interface IModelSelector
    {
        IModels GetModels(LlmModelsDto llmModelsDto);
    }
}