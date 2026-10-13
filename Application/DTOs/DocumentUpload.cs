namespace Application.DTOs
{
    public sealed class DocumentUpload(string fileName, Stream content)
    {
        public string FileName { get; } = fileName;
        public Task CopyToAsync(Stream destination)
        {
            // Storage and document readers consume the same upload in sequence.
            if (content.CanSeek) content.Position = 0;
            return content.CopyToAsync(destination);
        }
    }
}
