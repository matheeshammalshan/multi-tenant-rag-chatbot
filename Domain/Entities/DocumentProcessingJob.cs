namespace Domain.Entities
{
    public class DocumentProcessingJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int DocumentId { get; set; }
        public DocumentEntity Document { get; set; } = null !;
        public int Attempts { get; set; }
        public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
        public Guid? LeaseId { get; set; }
        public DateTime? LeaseUntilUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }
}
