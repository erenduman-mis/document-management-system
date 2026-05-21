using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DocumentManagementSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalDocuments = _context.Documents.Count(d => !d.IsDeleted);
            ViewBag.ActiveDocuments = _context.Documents.Count(d => !d.IsDeleted && d.Status == DocumentStatus.Approved);
            ViewBag.TotalUsers = _context.Users.Count(u => u.IsActive);
            ViewBag.TotalVersions = _context.DocumentVersions.Count();

            ViewBag.RecentDocuments = _context.Documents
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToList();

            var documentTypes = new[] { "AMM", "SB", "AD", "QA" };

            var typeCounts = _context.Documents
                .Where(d => !d.IsDeleted)
                .GroupBy(d => d.DocumentType)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToDictionary(x => x.Type, x => x.Count);

            ViewBag.DocumentTypeLabels = documentTypes.ToList();
            ViewBag.DocumentTypeCounts = documentTypes
                .Select(t => typeCounts.ContainsKey(t) ? typeCounts[t] : 0)
                .ToList();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}