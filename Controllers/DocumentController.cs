using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;

namespace DocumentManagementSystem.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index(
            string? search,
            string? type,
            string? status,
            string sortBy = "created",
            string sortOrder = "desc",
            int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Documents.Where(d => !d.IsDeleted);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(d => d.Title.Contains(search) || d.AircraftType.Contains(search));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(d => d.DocumentType == type);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DocumentStatus>(status, out var statusEnum))
                query = query.Where(d => d.Status == statusEnum);

            query = sortBy switch
            {
                "title" => sortOrder == "asc"
                    ? query.OrderBy(d => d.Title)
                    : query.OrderByDescending(d => d.Title),

                "type" => sortOrder == "asc"
                    ? query.OrderBy(d => d.DocumentType)
                    : query.OrderByDescending(d => d.DocumentType),

                "aircraft" => sortOrder == "asc"
                    ? query.OrderBy(d => d.AircraftType)
                    : query.OrderByDescending(d => d.AircraftType),

                "status" => sortOrder == "asc"
                    ? query.OrderBy(d => d.Status)
                    : query.OrderByDescending(d => d.Status),

                "created" => sortOrder == "asc"
                    ? query.OrderBy(d => d.CreatedAt)
                    : query.OrderByDescending(d => d.CreatedAt),

                _ => query.OrderByDescending(d => d.CreatedAt)
            };

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var documents = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Status = status;

            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(documents);
        }

        public IActionResult Details(int id)
        {
            var document = _context.Documents
                .FirstOrDefault(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            var versions = _context.DocumentVersions
                .Where(v => v.DocumentId == id)
                .OrderByDescending(v => v.VersionNumber)
                .ToList();

            var logs = _context.AuditLogs
                .Where(a => a.DocumentId == id)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            ViewBag.Versions = versions;
            ViewBag.Logs = logs;

            return View(document);
        }

        [Authorize(Roles = "Admin,Engineer,Quality")]
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Engineer,Quality")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            string title,
            string documentType,
            string aircraftType,
            string ataChapter,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please select a file.";
                return View();
            }

            var allowedTypes = new[] { ".pdf", ".docx", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedTypes.Contains(ext))
            {
                ViewBag.Error = "Only PDF, DOCX and XLSX files are allowed.";
                return View();
            }

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var hashValue = await CalculateSha256Hash(filePath);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var document = new Document
            {
                Title = title,
                DocumentType = documentType,
                AircraftType = aircraftType,
                ATAChapter = ataChapter,
                CreatedBy = userId,
                Status = DocumentStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = 1,
                FilePath = $"/uploads/{fileName}",
                FileType = ext.TrimStart('.').ToUpper(),
                FileSize = file.Length,
                HashValue = hashValue,
                IsObsolete = false,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            document.CurrentVersionId = version.Id;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                DocumentId = document.Id,
                DocumentVersionId = version.Id,
                ActionType = AuditActionType.Upload,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Details = $"Document uploaded: {title} v1"
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Engineer,Quality")]
        [HttpGet]
        public IActionResult UploadNewVersion(int id)
        {
            var document = _context.Documents
                .FirstOrDefault(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            return View(document);
        }

        [Authorize(Roles = "Admin,Engineer,Quality")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadNewVersion(int id, IFormFile file)
        {
            var document = _context.Documents
                .FirstOrDefault(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please select a file.";
                return View(document);
            }

            var allowedTypes = new[] { ".pdf", ".docx", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedTypes.Contains(ext))
            {
                ViewBag.Error = "Only PDF, DOCX and XLSX files are allowed.";
                return View(document);
            }

            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var hashValue = await CalculateSha256Hash(filePath);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var existingVersions = _context.DocumentVersions
                .Where(v => v.DocumentId == document.Id)
                .ToList();

            foreach (var oldVersion in existingVersions)
            {
                oldVersion.IsObsolete = true;
            }

            var nextVersionNumber = existingVersions.Any()
                ? existingVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var newVersion = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = nextVersionNumber,
                FilePath = $"/uploads/{fileName}",
                FileType = ext.TrimStart('.').ToUpper(),
                FileSize = file.Length,
                HashValue = hashValue,
                IsObsolete = false,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            document.CurrentVersionId = newVersion.Id;
            document.Status = DocumentStatus.Draft;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                DocumentId = document.Id,
                DocumentVersionId = newVersion.Id,
                ActionType = AuditActionType.Upload,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Details = $"New version uploaded: {document.Title} v{newVersion.VersionNumber}"
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = document.Id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id && !d.IsDeleted);

            if (document != null)
            {
                document.IsDeleted = true;

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    DocumentId = document.Id,
                    ActionType = AuditActionType.Delete,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Details = $"Document deleted: {document.Title}"
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Quality")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, DocumentStatus status)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            if (document.Status == status)
            {
                TempData["Error"] = $"Document is already {status}.";
                return RedirectToAction("Details", new { id });
            }

            var oldStatus = document.Status;
            document.Status = status;

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                DocumentId = document.Id,
                ActionType = AuditActionType.StatusChange,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Details = $"Status changed: {oldStatus} → {status}"
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Document status changed to {status}.";

            return RedirectToAction("Details", new { id });
        }

        private static async Task<string> CalculateSha256Hash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = System.IO.File.OpenRead(filePath);

            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes);
        }
    }
}