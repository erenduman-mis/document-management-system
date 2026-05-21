using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? search, string? actionType, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(actionType) &&
                Enum.TryParse<AuditActionType>(actionType, out var actionEnum))
            {
                query = query.Where(a => a.ActionType == actionEnum);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    (a.Details != null && a.Details.Contains(search)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(search)) ||
                    (a.User != null && (a.User.Name.Contains(search) || a.User.Email.Contains(search))) ||
                    (a.Document != null && a.Document.Title.Contains(search))
                );
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages == 0)
                totalPages = 1;

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var logs = query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogListViewModel
                {
                    Id = a.Id,
                    ActionType = a.ActionType.ToString(),
                    UserName = a.User != null ? a.User.Name : "-",
                    UserEmail = a.User != null ? a.User.Email : "-",
                    DocumentTitle = a.Document != null ? a.Document.Title : "-",
                    IpAddress = a.IpAddress ?? "-",
                    Details = a.Details ?? "-",
                    Timestamp = a.Timestamp
                })
                .ToList();

            ViewBag.Search = search;
            ViewBag.ActionType = actionType;
            ViewBag.ActionTypes = Enum.GetNames(typeof(AuditActionType)).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(logs);
        }
    }

    public class AuditLogListViewModel
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string DocumentTitle { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public string Details { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}