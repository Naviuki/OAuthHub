using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuthHub.Models.Entities;
using OAuthHub.Models.ViewModels;
using OAuthHub.Services;

namespace OAuthHub.Controllers;

[Authorize]
public class SessionController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISessionService _sessionService;

    public SessionController(
        UserManager<ApplicationUser> userManager,
        ISessionService sessionService)
    {
        _userManager = userManager;
        _sessionService = sessionService;
    }

    // GET: /Session
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var sessions = await _sessionService.GetUserSessionsAsync(user.Id);
        var currentSession = await _sessionService.FindCurrentSessionAsync(user.Id, HttpContext);

        var model = new SessionListViewModel
        {
            CurrentSessionId = currentSession?.Id,
            Sessions = sessions.Select(s => new SessionItemViewModel
            {
                Id = s.Id,
                IpAddress = s.IpAddress,
                DeviceInfo = s.DeviceInfo,
                LoginTime = s.LoginTime,
                LastActivityTime = s.LastActivityTime,
                IsActive = s.IsActive,
                IsCurrent = s.Id == currentSession?.Id
            }).ToList()
        };

        return View(model);
    }

    // POST: /Session/Terminate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int sessionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        await _sessionService.TerminateSessionAsync(sessionId, user.Id);
        TempData["Success"] = "Сессия завершена.";
        return RedirectToAction("Index");
    }

    // POST: /Session/TerminateAll
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TerminateAll()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var currentSession = await _sessionService.FindCurrentSessionAsync(user.Id, HttpContext);
        await _sessionService.TerminateAllSessionsExceptAsync(user.Id, currentSession?.Id);

        TempData["Success"] = "Все остальные сессии завершены.";
        return RedirectToAction("Index");
    }
}