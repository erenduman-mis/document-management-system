namespace DocumentManagementSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? DocumentId { get; set; }
        public Document? Document { get; set; }

        public int? DocumentVersionId { get; set; }
        public DocumentVersion? DocumentVersion { get; set; }

        public AuditActionType ActionType { get; set; }

        public DateTime Timestamp { get; set; }

        public string? IpAddress { get; set; }
        public string? Details { get; set; }
    }

    public enum AuditActionType
    {
        View,
        Download,
        Upload,
        Delete,
        StatusChange,
        Approve,
        Reject,
        Login,
        Logout
    }
}