using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuthHub.Models.Entities;
using OAuthHub.Models.ViewModels;
using OAuthHub.Services;

namespace OAuthHub.Controllers;

[Authorize]
public class ExportImportController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IExportService _exportService;

    public ExportImportController(
        UserManager<ApplicationUser> userManager,
        IExportService exportService)
    {
        _userManager = userManager;
        _exportService = exportService;
    }

    // GET: /ExportImport
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var logins = await _userManager.GetLoginsAsync(user);

        var model = new ExportDataViewModel
        {
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            LinkedProviders = logins.Select(l => l.LoginProvider).ToList(),
            StatusMessage = TempData["Success"]?.ToString()
        };

        return View(model);
    }

    // GET: /ExportImport/DownloadJson
    [HttpGet]
    public async Task<IActionResult> DownloadJson()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var json = await _exportService.ExportUserDataAsJsonAsync(user.Id);
        var bytes = Encoding.UTF8.GetBytes(json);

        return File(bytes, "application/json", $"oauthhub_export_{DateTime.UtcNow:yyyyMMdd}.json");
    }

    // GET: /ExportImport/DownloadCsv
    [HttpGet]
    public async Task<IActionResult> DownloadCsv()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var csv = await _exportService.ExportUserDataAsCsvAsync(user.Id);
        var bytes = Encoding.UTF8.GetBytes(csv);

        return File(bytes, "text/csv", $"oauthhub_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}