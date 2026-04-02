using Microsoft.EntityFrameworkCore;
using OAuthHub.Data;
using OAuthHub.Models.Entities;

namespace OAuthHub.Services;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;

    public SessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession> CreateSessionAsync(string userId, HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var session = new UserSession
        {
            UserId = userId,
            IpAddress = ip,
            UserAgent = userAgent,
            DeviceInfo = ParseDeviceInfo(userAgent),
            LoginTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow,
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<IList<UserSession>> GetUserSessionsAsync(string userId)
    {
        return await _context.UserSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityTime)
            .ToListAsync();
    }

    public async Task TerminateSessionAsync(int sessionId, string userId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session != null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task TerminateAllSessionsExceptAsync(string userId, int? exceptSessionId = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.Id != (exceptSessionId ?? 0))
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateLastActivityAsync(int sessionId)
    {
        var session = await _context.UserSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastActivityTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserSession?> FindCurrentSessionAsync(string userId, HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        return await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.IpAddress == ip && s.UserAgent == userAgent)
            .OrderByDescending(s => s.LoginTime)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsSessionActiveAsync(int sessionId)
    {
        return await _context.UserSessions
            .AnyAsync(s => s.Id == sessionId && s.IsActive);
    }

    public string ParseDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Неизвестное устройство";

        var ua = userAgent.ToLowerInvariant();

        string os;
        if (ua.Contains("windows nt 10")) os = "Windows 10/11";
        else if (ua.Contains("windows")) os = "Windows";
        else if (ua.Contains("macintosh") || ua.Contains("mac os")) os = "macOS";
        else if (ua.Contains("android")) os = "Android";
        else if (ua.Contains("iphone") || ua.Contains("ipad")) os = "iOS";
        else if (ua.Contains("linux")) os = "Linux";
        else os = "Неизвестная ОС";

        string browser;
        if (ua.Contains("edg/")) browser = "Edge";
        else if (ua.Contains("chrome") && !ua.Contains("edg")) browser = "Chrome";
        else if (ua.Contains("firefox")) browser = "Firefox";
        else if (ua.Contains("safari") && !ua.Contains("chrome")) browser = "Safari";
        else if (ua.Contains("opera") || ua.Contains("opr/")) browser = "Opera";
        else browser = "Неизвестный браузер";

        return $"{browser} на {os}";
    }
}