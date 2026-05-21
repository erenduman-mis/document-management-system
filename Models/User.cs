namespace DocumentManagementSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();

        public ICollection<DocumentVersion> UploadedVersions { get; set; } = new List<DocumentVersion>();
    }
}