using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ILlmConfigService
    {
         Task<LlmConfigurationEntity> CreateConfig(LlmConfigurationsDto configDto);
         Task<LlmConfigurationEntity> DefaultConfig(LlmConfigurationsDto configDto);
    }
}