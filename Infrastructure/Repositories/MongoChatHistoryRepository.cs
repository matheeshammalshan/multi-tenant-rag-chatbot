using Application.Interfaces.Repositories;
using Application.DTOs;
using Infrastructure.DTOs;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Repositories
{
    public sealed class MongoChatHistoryRepository : IChatHistoryRepository
    {
        private readonly IMongoClient client;
        private readonly IConfiguration configuration;
        public MongoChatHistoryRepository(IMongoClient client, IConfiguration configuration)
        {
            this.client = client;
            this.configuration = configuration;
        }

        private IMongoCollection<ChatHistoryDto> Collection
        {
            get
            {
                return client.GetDatabase(configuration["MongoDB:Database"] ?? "Rag").GetCollection<ChatHistoryDto>("chatHistory");
            }
        }

        public Task AddAsync(int chatBotId, string userQuery, string systemQuery)
        {
            return AddConversationAsync(chatBotId, Guid.NewGuid(), userQuery, systemQuery);
        }

        public Task AddConversationAsync(int chatBotId, Guid conversationId, string userQuery, string systemQuery)
        {
            return Collection.InsertOneAsync(new ChatHistoryDto { ChatBotId = chatBotId, ConversationId = conversationId, CreatedAtUtc = DateTime.UtcNow, UserQuery = userQuery, SystemQuery = systemQuery });
        }

        public async Task<List<ChatHistoryResponseDto>> GetPageAsync(int chatBotId, Guid? conversationId, int skip, int take)
        {
            var filter = Builders<ChatHistoryDto>.Filter.Eq(x => x.ChatBotId, chatBotId);
            if (conversationId is Guid id)
            {
                filter &= Builders<ChatHistoryDto>.Filter.Eq(x => x.ConversationId, id);
            }

            var items = await Collection.Find(filter).SortByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(skip).Limit(take).ToListAsync();
            return items.Select(x => new ChatHistoryResponseDto(x.UserQuery, x.SystemQuery) { Id = x.Id.ToString(), ConversationId = x.ConversationId, CreatedAtUtc = x.CreatedAtUtc }).ToList();
        }

        public async Task<List<ChatHistoryResponseDto>> GetAsync(int chatBotId)
        {
            using var cursor = await Collection.FindAsync(x => x.ChatBotId == chatBotId);
            return (await cursor.ToListAsync()).Select(x => new ChatHistoryResponseDto(x.UserQuery, x.SystemQuery)).ToList();
        }
    }
}
