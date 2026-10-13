using Application.DTOs;
namespace Application.Interfaces.Integrations
{
    public interface IDocumentNotifier
    {
        Task NotifyAsync(DocumentResponseDto response);
    }
}