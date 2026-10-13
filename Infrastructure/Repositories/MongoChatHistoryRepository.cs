using Application.Interfaces.Repositories;
using Application.DTOs;
using Infrastructure.DTOs;
using MongoDB.Driver;
namespace Infrastructure.Repositories
{
    public sealed class MongoChatHistoryRepository(IMongoClient client) : IChatHistoryRepository
    {
        private IMongoCollection<ChatHistoryDto> Collection => client.GetDatabase("Rag").GetCollection<ChatHistoryDto>("chatHistory");
        public Task AddAsync(int chatBotId, string userQuery, string systemQuery) => Collection.InsertOneAsync(new ChatHistoryDto { ChatBotId = chatBotId, UserQuery = userQuery, SystemQuery = systemQuery });
        public async Task<List<ChatHistoryResponseDto>> GetAsync(int chatBotId)
        {
            using var cursor = await Collection.FindAsync(x => x.ChatBotId == chatBotId);
            return (await cursor.ToListAsync()).Select(x => new ChatHistoryResponseDto(x.UserQuery, x.SystemQuery)).ToList();
        }
    }
}