namespace OAuthHub.Models.Entities;

/// <summary>
/// Сущность для хранения истории и активных сессий пользователя.
/// </summary>
public class UserSession
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Краткое описание браузера/устройства.</summary>
    public string? DeviceInfo { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    /// <summary>Идентификатор Security Stamp на момент создания сессии (для инвалидации).</summary>
    public string? SecurityStamp { get; set; }
}