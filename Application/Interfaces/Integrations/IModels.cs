using Application.DTOs;
using Domain.Enums;
using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IModels
    {
        bool CheckModelType(LlmModelsDto llmModelsDto);
        Task<string> CreateChat(string userQuery,LlmConfigurationsDto configDto,string words);
    }
}