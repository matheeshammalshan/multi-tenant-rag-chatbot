namespace Application.DTOs
{
    public class ChatStreamEvent
    {
        public string Type { get; set; }
        public string? Text { get; set; }
        public Guid? ConversationId { get; set; }

        public ChatStreamEvent(string Type, string? Text = null, Guid? ConversationId = null)
        {
            this.Type = Type;
            this.Text = Text;
            this.ConversationId = ConversationId;
        }
    }
}
