namespace Application.DTOs
{
    public class ChatHistoryResponseDto
    {
        public string? UserPromt { get; set; }
        public string? SystemMessage { get; set; }
        public string? Id { get; set; }
        public Guid ConversationId { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public ChatHistoryResponseDto(string? userPromt, string? systemMessage)
        {
            UserPromt = userPromt;
            SystemMessage = systemMessage;
        }
    }
}
