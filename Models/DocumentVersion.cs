namespace DocumentManagementSystem.Models
{
    public class DocumentVersion
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int VersionNumber { get; set; }

        public string FilePath { get; set; } = null!;

        public string FileType { get; set; } = null!;

        public long FileSize { get; set; }

        public string HashValue { get; set; } = null!;

        public bool IsObsolete { get; set; } = false;

        public int UploadedBy { get; set; }
        public User Uploader { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PublishedDate { get; set; }

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}