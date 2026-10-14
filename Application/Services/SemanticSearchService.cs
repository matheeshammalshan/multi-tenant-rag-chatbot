using System.Runtime.CompilerServices;
using System.Text;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class SemanticSearchService : ISemanticSearchService
    {
        private readonly IEmbeddingsCreater embeddings;
        private readonly IRetriveEmbeddings retrieval;
        private readonly IApplicationRepository repository;
        private readonly IModelSelector selector;
        private readonly IChatHistoryRepository history;
        private readonly IResourceAccessService access;
        public SemanticSearchService(IEmbeddingsCreater embeddings, IRetriveEmbeddings retrieval, IApplicationRepository repository, IModelSelector selector, IChatHistoryRepository history, IResourceAccessService access)
        {
            this.embeddings = embeddings;
            this.retrieval = retrieval;
            this.repository = repository;
            this.selector = selector;
            this.history = history;
            this.access = access;
        }

        public async Task<string> SearchEmbeddings(SematicSearchingDto request)
        {
            var answer = new StringBuilder();
            await foreach (var part in StreamAsync(request))
            {
                if (part.Type == "token")
                {
                    answer.Append(part.Text);
                }
            }

            return answer.ToString();
        }

        public IAsyncEnumerable<ChatStreamEvent> StreamAsync(SematicSearchingDto request, CancellationToken cancellationToken = default)
        {
            return StreamCoreAsync(request, false, cancellationToken);
        }

        public IAsyncEnumerable<ChatStreamEvent> StreamForConsoleAsync(SematicSearchingDto request, CancellationToken cancellationToken = default)
        {
            return StreamCoreAsync(request, true, cancellationToken);
        }

        private async IAsyncEnumerable<ChatStreamEvent> StreamCoreAsync(SematicSearchingDto request, bool consoleRequest, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Domain.Entities.ChatBotEntity? bot;
            if (consoleRequest)
            {
                bot = await access.RequireChatbotAsync(request.ChatBotId);
            }
            else
            {
                bot = await repository.FindChatBotAsync(request.ChatBotId);
                if (bot == null)
                {
                    throw new ApiException(401, "Invalid chatbot API key.");
                }
            }
            if (!consoleRequest && !ApiKeyService.IsValid(bot, request.ApiKey))
            {
                throw new ApiException(401, "Invalid chatbot API key.");
            }

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ApiException(400, "A question is required.");
            }

            if (request.ConversationId == null)
            {
                request.ConversationId = Guid.NewGuid();
            }
            yield return new ChatStreamEvent("progress", "Embedding question", request.ConversationId);
            cancellationToken.ThrowIfCancellationRequested();
            var vector = await embeddings.CreateEmbeddings(request.Query);
            if (vector == null || vector.Length == 0)
            {
                throw new ApiException(502, "Embedding service returned no vector.");
            }

            yield return new ChatStreamEvent("progress", "Searching chatbot documents");
            var readyIds = await repository.ReadyDocumentIdsAsync(request.ChatBotId);
            string context = "";
            if (readyIds.Length > 0)
            {
                context = await retrieval.RetrieveReadyAsync(vector, request.ChatBotId, readyIds);
            }
            var answer = new StringBuilder();
            if (string.IsNullOrWhiteSpace(context))
            {
                var message = "I could not find relevant processed documents for this question.";
                answer.Append(message);
                yield return new ChatStreamEvent("token", message);
            }
            else
            {
                var configuration = await repository.FindConfigurationAsync(request.ChatBotId);
            if (configuration == null)
            {
                throw ApiException.NotFound("Model configuration");
            }
                var config = new LlmConfigurationsDto(configuration.ChatBotId, configuration.ModelName, configuration.Temperature, configuration.MaxToken, configuration.TopP);
                config.SystemPrompt = bot.SystemPromt;
                yield return new ChatStreamEvent("progress", "Generating answer");
                var model = selector.GetModels(config.ModelName);
                await foreach (var token in model.StreamChatAsync(request.Query, config, context, cancellationToken))
                {
                    answer.Append(token);
                    yield return new ChatStreamEvent("token", token);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await history.AddConversationAsync(request.ChatBotId, request.ConversationId.Value, request.Query, answer.ToString());
            await repository.RecordQueryAsync(request.ChatBotId);
            yield return new ChatStreamEvent("completed", answer.ToString(), request.ConversationId);
        }
    }
}
