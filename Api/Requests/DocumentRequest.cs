using Application.DTOs;
namespace Api.Requests
{
    public sealed class DocumentRequest
    {
        public int Id { get; set; }
        public int ChatBotId { get; set; }
        public string? Name { get; set; }
        public string? Size { get; set; }
        public DateOnly UploadDate { get; set; }
        public string? Status { get; set; }
        public required IFormFile File { get; set; }
        public string? FileUrl { get; set; }
        public ReaderType ReaderType { get; set; }
        public DocumentDto ToDto(Stream content) => new()
        {
            Id = Id, ChatBotId = ChatBotId, Name = Name, Size = Size,
            UploadDate = UploadDate, Status = Status, FileUrl = FileUrl,
            ReaderType = ReaderType, File = new DocumentUpload(File.FileName, content)
        };
    }
}