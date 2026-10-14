namespace Application.DTOs
{
    public class DocumentResponseDto
    {
        public string? Message { get; set; }
        public string? Status { get; set; }
        public int Chunks { get; set; }
        public string EventType { get; set; } = "document.status";
        public int DocumentId { get; set; }
        public int ChatBotId { get; set; }
        public string? Error { get; set; }
    }
}
