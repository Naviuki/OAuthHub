using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuthHub.Data;
using OAuthHub.Models.Entities;

namespace OAuthHub.Services;

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExportService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<string> ExportUserDataAsJsonAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return "{}";

        var logins = await _userManager.GetLoginsAsync(user);
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync();

        var exportData = new
        {
            Profile = new
            {
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.CreatedAt,
                user.TwoFactorEnabled
            },
            LinkedProviders = logins.Select(l => new
            {
                l.LoginProvider,
                l.ProviderDisplayName,
                l.ProviderKey
            }),
            Sessions = sessions.Select(s => new
            {
                s.IpAddress,
                s.DeviceInfo,
                s.LoginTime,
                s.LastActivityTime,
                s.IsActive
            })
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public async Task<string> ExportUserDataAsCsvAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return "";

        var logins = await _userManager.GetLoginsAsync(user);
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync();

        var sb = new StringBuilder();

        // Профиль
        sb.AppendLine("=== Профиль ===");
        sb.AppendLine("Email,Имя,Дата регистрации,2FA");
        sb.AppendLine($"{user.Email},{user.DisplayName},{user.CreatedAt:yyyy-MM-dd HH:mm:ss},{user.TwoFactorEnabled}");
        sb.AppendLine();

        // Привязанные аккаунты
        sb.AppendLine("=== Привязанные аккаунты ===");
        sb.AppendLine("Провайдер,Ключ");
        foreach (var login in logins)
        {
            sb.AppendLine($"{login.LoginProvider},{login.ProviderKey}");
        }
        sb.AppendLine();

        // Сессии
        sb.AppendLine("=== Сессии ===");
        sb.AppendLine("IP,Устройство,Вход,Последняя активность,Активна");
        foreach (var session in sessions)
        {
            sb.AppendLine($"{session.IpAddress},{session.DeviceInfo},{session.LoginTime:yyyy-MM-dd HH:mm:ss},{session.LastActivityTime:yyyy-MM-dd HH:mm:ss},{session.IsActive}");
        }

        return sb.ToString();
    }
}