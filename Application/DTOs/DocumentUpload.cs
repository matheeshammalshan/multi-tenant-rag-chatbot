namespace Application.DTOs
{
    public sealed class DocumentUpload
    {
        private readonly Stream content;
        public DocumentUpload(string fileName, Stream content)
        {
            this.content = content;
            this.FileName = fileName;
        }

        public string FileName { get; }

        public long? Length
        {
            get
            {
                return content.CanSeek ? content.Length : null;
            }
        }

        public Task CopyToAsync(Stream destination)
        {
            // Storage and document readers consume the same upload in sequence.
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            return content.CopyToAsync(destination);
        }
    }
}
