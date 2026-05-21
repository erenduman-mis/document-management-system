namespace DocumentManagementSystem.Models
{
    public class Document
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string DocumentType { get; set; } = null!;

        public string AircraftType { get; set; } = null!;

        public string ATAChapter { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

        public int CreatedBy { get; set; }
        public User Creator { get; set; } = null!;

        public int? CurrentVersionId { get; set; }
        public DocumentVersion? CurrentVersion { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public enum DocumentStatus
    {
        Draft,
        InReview,
        Approved,
        Rejected
    }
}