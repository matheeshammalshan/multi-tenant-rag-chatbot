using System.Text.Json;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;
[ApiController, Route("ChatBots/{id:int}/stream"), Authorize, EnableRateLimiting("Chat")]
public class PlaygroundController : ControllerBase
{
    private readonly ISemanticSearchService search;
    private readonly IResourceAccessService access;
    public PlaygroundController(ISemanticSearchService search, IResourceAccessService access)
    {
        this.search = search;
        this.access = access;
    }

    [HttpPost, Produces("text/event-stream")]
    public async Task Stream(int id, SematicSearchingDto request, CancellationToken cancellationToken)
    {
        await access.RequireChatbotAsync(id);
        request.ChatBotId = id;
        Response.ContentType = "text/event-stream";
        try
        {
            await foreach (var part in search.StreamForConsoleAsync(request, cancellationToken))
            {
                await WriteEvent(part, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<PlaygroundController>>().LogError(exception, "Playground chat failed");
            await WriteEvent(new ChatStreamEvent("error", exception is ApiException ? exception.Message : "Chat generation failed."), cancellationToken);
        }
    }

    private async Task WriteEvent(ChatStreamEvent part, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(part, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Response.WriteAsync($"event: {part.Type}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
