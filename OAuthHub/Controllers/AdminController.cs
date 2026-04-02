using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OAuthHub.Data;
using OAuthHub.Models.Entities;
using OAuthHub.Services;

namespace OAuthHub.Controllers;

[Authorize(Roles = "Admin")] // Только для админов
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISessionService _sessionService;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISessionService sessionService)
    {
        _context = context;
        _userManager = userManager;
        _sessionService = sessionService;
    }

    // Сводная статистика и панель (Dashboard)
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.ActiveSessions = await _context.UserSessions.CountAsync(s => s.IsActive);
        ViewBag.TotalLogs = await _context.AuditLogs.CountAsync();

        var recentLogs = await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToListAsync();

        return View(recentLogs);
    }

    // Просмотр и бан пользователей
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Нельзя заблокировать самого себя!";
            return RedirectToAction("Users");
        }

        bool isBlocked = await _userManager.IsLockedOutAsync(user);

        if (isBlocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Пользователь {user.Email} разблокирован.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Бан
            await _sessionService.TerminateAllSessionsExceptAsync(user.Id); // Сброс сессий
            TempData["Success"] = $"Пользователь {user.Email} заблокирован.";
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserEmail = User.Identity?.Name,
            Action = isBlocked ? "Разблокировка" : "Блокировка",
            Details = $"Пользователь {user.Email}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _context.SaveChangesAsync();

        return RedirectToAction("Users");
    }

    // Список всех сессий в системе
    public async Task<IActionResult> Sessions()
    {
        var sessions = await _context.UserSessions
            .Include(s => s.User)
            .OrderByDescending(s => s.LastActivityTime)
            .ToListAsync();

        return View(sessions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TerminateSession(int sessionId)
    {
        var session = await _context.UserSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session != null)
        {
            session.IsActive = false;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = _userManager.GetUserId(User),
                UserEmail = User.Identity?.Name,
                Action = "Сброс сессии админом",
                Details = $"Сессия ID: {sessionId} пользователя {session.User.Email}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Сессия принудительно завершена.";
        return RedirectToAction("Sessions");
    }

    // Просмотр логов
    public async Task<IActionResult> Logs()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(100) // 100 последних
            .ToListAsync();

        return View(logs);
    }
}