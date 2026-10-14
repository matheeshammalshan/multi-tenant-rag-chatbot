using Application.DTOs;
using Domain.Enums;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Application.Interfaces.Integrations
{
    public interface IModels
    {
        bool CheckModelType(LlmModelsDto llmModelsDto);
        Task<string> CreateChat(string userQuery, LlmConfigurationsDto configDto, string words);
        async IAsyncEnumerable<string> StreamChatAsync(string query, LlmConfigurationsDto config, string context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return await CreateChat(query, config, context);
        }
    }
}
