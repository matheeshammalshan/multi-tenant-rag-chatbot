using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ISemanticSearchService
    {
        Task<string> SearchEmbeddings(SematicSearchingDto semanticSearchingDto);
    }
}