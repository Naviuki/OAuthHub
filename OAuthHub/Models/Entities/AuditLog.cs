namespace OAuthHub.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = null!; // "Вход", "Выход", "Блокировка пользователя"
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}