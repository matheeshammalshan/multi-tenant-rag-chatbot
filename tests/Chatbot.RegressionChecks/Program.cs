using Application.Interfaces.Integrations;
using Application.Configuration;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Api.Filters;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.DocumentReaders;
using Infrastructure.LlmModels;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Application.Exceptions;
using Application.Security;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS: {message}");
}

var applicationReferences = typeof(IApplicationRepository).Assembly.GetReferencedAssemblies();
Check(!applicationReferences.Any(x => x.Name!.Contains("Infrastructure") || x.Name.Contains("EntityFrameworkCore") || x.Name.Contains("MongoDB") || x.Name.Contains("Qdrant") || x.Name.Contains("AspNetCore")), "Application has no infrastructure or HTTP dependencies");
Check(!typeof(ChatBotEntity).Assembly.GetReferencedAssemblies().Any(x => x.Name is "Application" or "Infrastructure" or "Api"), "Domain has no dependencies on other layers");

var services = new ServiceCollection();
services.AddLogging();
services.AddApplication();
services.AddInfrastructure();
services.AddScoped<ICurrentUser, TestCurrentUser>();
using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
using var scope = provider.CreateScope();
foreach (var serviceType in new[] { typeof(IOrganizationService), typeof(IChatBotService), typeof(ILlmConfigService), typeof(IDocumentService), typeof(ISemanticSearchService), typeof(IApiKeyService), typeof(IChatHistoryService) })
    _ = scope.ServiceProvider.GetRequiredService(serviceType);
Check(true, "All application services resolve with scoped dependency validation");

using var source = new MemoryStream(Encoding.UTF8.GetBytes("First sentence. Second sentence!"));
var document = new DocumentDto { ChatBotId = 7, File = new DocumentUpload("document.txt", source), ReaderType = ReaderType.Text };
using var saved = new MemoryStream();
await document.File.CopyToAsync(saved);
var chunks = await new TextDocumentReader().CreateText(document);
Check(chunks.SequenceEqual(new[] { "First sentence", "Second sentence" }), "Text ingestion reads the complete upload after file storage");
using var secondCopy = new MemoryStream();
await document.File.CopyToAsync(secondCopy);
Check(saved.ToArray().SequenceEqual(secondCopy.ToArray()), "Repeated upload consumers receive identical content");

var store = new FakeApplicationRepository();
var embeddings = new FakeEmbeddings();
var retrieval = new FakeRetrieval();
var history = new FakeChatHistoryRepository();
var search = new SemanticSearchService(embeddings, retrieval, store, new FakeModelSelector(), history, scope.ServiceProvider.GetRequiredService<IResourceAccessService>());
var request = new SematicSearchingDto { ChatBotId = 7, ApiKey = "wrong", Query = "question" };
try { await search.SearchEmbeddings(request); throw new Exception("Invalid key was accepted"); }
catch (ApiException exception) { Check(exception.StatusCode == 401 && embeddings.Calls == 0 && history.Calls == 0, "Invalid API keys stop before embedding generation and history persistence"); }
request.ChatBotId = 8;
try { await search.SearchEmbeddings(request); throw new Exception("Unknown chatbot was accepted"); }
catch (ApiException exception) { Check(exception.StatusCode == 401 && embeddings.Calls == 0, "Unknown chatbot IDs stop before external integrations"); }
request.ChatBotId = 7;
request.ApiKey = "valid";
Check(await search.SearchEmbeddings(request) == "answer" && retrieval.ChatBotId == 7 && history.ChatBotId == 7 && history.Calls == 1, "Successful search propagates chatbot scope and persists its response");
Check(await new ChatHistoryService(history).RetriveAllChatHistory(7) is [{ SystemMessage: "answer" }], "Chat history service returns the storage result");
var callsBeforeConsole = embeddings.Calls;
try
{
    await foreach (var part in search.StreamForConsoleAsync(new() { ChatBotId = 7, Query = "unauthenticated" })) { }
    throw new Exception("Unauthenticated playground chat was accepted");
}
catch (ApiException exception) { Check(exception.StatusCode == 401 && embeddings.Calls == callsBeforeConsole, "Playground authorization stops before external integrations"); }
Console.WriteLine("All regression checks passed. No external services were contacted.");

sealed class FakeApplicationRepository : IApplicationRepository
{
    public Task<ChatBotEntity?> FindChatBotAsync(int id) => Task.FromResult(id == 7 ? new ChatBotEntity { Id = 7, ApiKeyHash = SecretTokens.Hash("valid") } : null);
    public Task<ChatBotEntity?> FindChatBotByApiKeyAsync(string key) => FindChatBotAsync(7);
    public Task<DocumentEntity?> FindDocumentAsync(int id) => throw new NotSupportedException();
    public Task RecordQueryAsync(int chatbotId) => Task.CompletedTask;
    public Task<int[]> ReadyDocumentIdsAsync(int chatbotId) => Task.FromResult(new[] { 123 });
    public Task<LlmConfigurationEntity?> FindConfigurationAsync(int chatBotId) => Task.FromResult<LlmConfigurationEntity?>(new() { ChatBotId = chatBotId, ModelName = LlmModelsDto.KimiK2 });
    public Task AddAsync<T>(T entity) where T : class => throw new NotSupportedException();
    public void Update<T>(T entity) where T : class => throw new NotSupportedException();
    public Task<int> SaveChangesAsync() => throw new NotSupportedException();
    public Task<IApplicationTransaction> BeginTransactionAsync() => throw new NotSupportedException();
}

sealed class FakeEmbeddings : IEmbeddingsCreater
{
    public int Calls { get; private set; }
    public Task<float[]?> CreateEmbeddings(string promt) { Calls++; return Task.FromResult<float[]?>([1f]); }
}

sealed class FakeRetrieval : IRetriveEmbeddings
{
    public int ChatBotId { get; private set; }
    public Task<string> RetriveEmbeddingsAsWords(float[] queary, int chatbotId) { ChatBotId = chatbotId; return Task.FromResult("context"); }
}

sealed class FakeModelSelector : IModelSelector
{
    public IModels GetModels(LlmModelsDto model) => new FakeModel();
    private sealed class FakeModel : IModels
    {
        public bool CheckModelType(LlmModelsDto model) => true;
        public Task<string> CreateChat(string userQuery, LlmConfigurationsDto configDto, string words) => Task.FromResult("answer");
    }
}

sealed class FakeChatHistoryRepository : IChatHistoryRepository
{
    public int Calls { get; private set; }
    public int ChatBotId { get; private set; }
    private readonly List<ChatHistoryResponseDto> _items = [];
    public Task AddAsync(int chatBotId, string userQuery, string systemQuery)
    {
        Calls++; ChatBotId = chatBotId;
        _items.Add(new(userQuery, systemQuery));
        return Task.CompletedTask;
    }
    public Task<List<ChatHistoryResponseDto>> GetAsync(int chatBotId) => Task.FromResult(_items);
    public Task AddConversationAsync(int chatBotId, Guid conversationId, string userQuery, string systemQuery) => AddAsync(chatBotId, userQuery, systemQuery);
    public Task<List<ChatHistoryResponseDto>> GetPageAsync(int chatBotId, Guid? conversationId, int skip, int take) => GetAsync(chatBotId);
}

sealed class TestCurrentUser : ICurrentUser
{
    public Guid? Id => null;
    public int? OrganizationId => null;
    public bool IsOwner => false;
}
