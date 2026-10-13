namespace Application.DTOs
{
    public class DocumentResponseDto
    {
        public string? Message { get; set; }
        public string? Status { get; set; }
        public int Chunks { get; set; }
    }
}