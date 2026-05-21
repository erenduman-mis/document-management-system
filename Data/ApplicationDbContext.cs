using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Quality" },
                new Role { Id = 3, RoleName = "Engineer" },
                new Role { Id = 4, RoleName = "Viewer" }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@aviation.com",
                    PasswordHash = "PrP+ZrMeO00Q+nC1ytSccRIpSvauTkdqHEBRVdRaoSE=",
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Creator)
                .WithMany(u => u.CreatedDocuments)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.CurrentVersion)
                .WithMany()
                .HasForeignKey(d => d.CurrentVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentVersion>()
                .HasOne(v => v.Uploader)
                .WithMany(u => u.UploadedVersions)
                .HasForeignKey(v => v.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Document)
                .WithMany(d => d.AuditLogs)
                .HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.DocumentVersion)
                .WithMany(v => v.AuditLogs)
                .HasForeignKey(a => a.DocumentVersionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasConversion<int>();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.ActionType)
                .HasConversion<int>();
        }
    }
}