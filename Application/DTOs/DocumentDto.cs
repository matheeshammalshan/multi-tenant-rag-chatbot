using System;


namespace Application.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int ChatBotId { get; set; }
        public string? Name { get; set; }
        public string? Size { get; set; }
        public DateOnly UploadDate { get; set; }
        public string? Status { get; set; }
        public required DocumentUpload File { get; set; }
        public string? FileUrl { get; set; }
    
        public ReaderType ReaderType { get; set; }
    
    }
}