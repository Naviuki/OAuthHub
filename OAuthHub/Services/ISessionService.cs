using OAuthHub.Models.Entities;

namespace OAuthHub.Services;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(string userId, HttpContext httpContext);
    Task<IList<UserSession>> GetUserSessionsAsync(string userId);
    Task TerminateSessionAsync(int sessionId, string userId);
    Task TerminateAllSessionsExceptAsync(string userId, int? exceptSessionId = null);
    Task UpdateLastActivityAsync(int sessionId);
    Task<UserSession?> FindCurrentSessionAsync(string userId, HttpContext httpContext);
    string ParseDeviceInfo(string? userAgent);

    /// <summary>Проверяет, активна ли сессия с данным Id.</summary>
    Task<bool> IsSessionActiveAsync(int sessionId);
}