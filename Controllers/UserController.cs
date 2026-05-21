using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocumentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users
                .Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    RoleName = _context.Roles
                        .Where(r => r.Id == u.RoleId)
                        .Select(r => r.RoleName)
                        .FirstOrDefault() ?? "Unknown"
                })
                .OrderBy(u => u.Name)
                .ToList();

            ViewBag.Roles = _context.Roles.OrderBy(r => r.RoleName).ToList();
            ViewBag.CurrentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = _context.Roles.OrderBy(r => r.RoleName).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string email, string password, int roleId)
        {
            ViewBag.Roles = _context.Roles.OrderBy(r => r.RoleName).ToList();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Name, email and password are required.";
                return View();
            }

            if (!_context.Roles.Any(r => r.Id == roleId))
            {
                ViewBag.Error = "Please select a valid role.";
                return View();
            }

            email = email.Trim().ToLower();

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "This email is already registered.";
                return View();
            }

            var user = new User
            {
                Name = name.Trim(),
                Email = email,
                PasswordHash = AccountController.HashPassword(password),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User created successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Index");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                TempData["Success"] = user.IsActive
                    ? "User activated."
                    : "User deactivated.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, int roleId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction("Index");
            }

            if (!_context.Roles.Any(r => r.Id == roleId))
            {
                TempData["Error"] = "Selected role is invalid.";
                return RedirectToAction("Index");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                user.RoleId = roleId;
                await _context.SaveChangesAsync();

                TempData["Success"] = "User role updated.";
            }

            return RedirectToAction("Index");
        }
    }

    public class UserListViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string RoleName { get; set; } = null!;
    }
}