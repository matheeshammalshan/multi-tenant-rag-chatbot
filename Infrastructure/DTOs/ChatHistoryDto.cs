using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Infrastructure.DTOs
{
    public class ChatHistoryDto
    {
        public ObjectId Id { get; set; }
        public int ChatBotId { get; set; }
        public string? UserQuery { get; set; }
        public string? SystemQuery { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ConversationId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
