using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IApiKeyService
    {
        Task<string> GenerateApiKey(ApiKeyDto dto);
        Task<ChatbotAccessDto> ResolveAsync(string key);
    }
}
