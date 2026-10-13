using MongoDB.Bson;

namespace Infrastructure.DTOs
{
    public class ChatHistoryDto
    {
        public ObjectId Id { get; set; }
        public int ChatBotId { get; set; }
        public string? UserQuery { get; set; }
        public string? SystemQuery { get; set; }
    
    }
}