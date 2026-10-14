using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto> CreateDocument(DocumentDto documentDto);
    }
}
