using System.Text.Json;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Services;
using Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController, Route("SemanticSearch"), AllowAnonymous, RequiredApiKey, EnableRateLimiting("Chat")]
    public class SemanticSearchController : ControllerBase
    {
        private readonly ISemanticSearchService service;
        public SemanticSearchController(ISemanticSearchService service)
        {
            this.service = service;
        }

        [HttpPost("askQuery")]
        public async Task<ChatAnswerDto> Ask(SematicSearchingDto request)
        {
            Prepare(request);
            var answer = await service.SearchEmbeddings(request);
            return new ChatAnswerDto(request.ChatBotId, request.ConversationId!.Value, answer);
        }

        [HttpPost("stream"), Produces("text/event-stream")]
        public async Task Stream(SematicSearchingDto request, CancellationToken cancellationToken)
        {
            Prepare(request);
            Response.ContentType = "text/event-stream";
            try
            {
                await foreach (var part in service.StreamAsync(request, cancellationToken))
                {
                    await WriteEvent(part, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<SemanticSearchController>>().LogError(exception, "Streaming chat failed");
                await WriteEvent(new ChatStreamEvent("error", exception is ApiException ? exception.Message : "Chat generation failed."), cancellationToken);
            }
        }

        private void Prepare(SematicSearchingDto request)
        {
            var id = (int)HttpContext.Items["ChatbotId"]!;
            if (request.ChatBotId != 0 && request.ChatBotId != id)
            {
                throw new ApiException(403, "The key belongs to a different chatbot.");
            }

            request.ChatBotId = id;
            request.ApiKey = (string)HttpContext.Items["ApiKey"]!;
        }

        private async Task WriteEvent(ChatStreamEvent part, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(part, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await Response.WriteAsync($"event: {part.Type}\ndata: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
