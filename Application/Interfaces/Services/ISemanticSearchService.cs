using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ISemanticSearchService
    {
        IAsyncEnumerable<ChatStreamEvent> StreamForConsoleAsync(SematicSearchingDto request, CancellationToken cancellationToken = default);
        Task<string> SearchEmbeddings(SematicSearchingDto semanticSearchingDto);
        IAsyncEnumerable<ChatStreamEvent> StreamAsync(SematicSearchingDto request, CancellationToken cancellationToken = default);
    }
}
