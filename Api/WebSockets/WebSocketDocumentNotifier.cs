using Application.Interfaces.Integrations;
using Application.DTOs;
namespace Api.WebSockets
{
    public sealed class WebSocketDocumentNotifier : IDocumentNotifier
    {
        public Task NotifyAsync(DocumentResponseDto response) => WebSocketManager.SendMessageToAll(response);
    }
}