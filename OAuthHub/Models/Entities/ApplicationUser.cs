using Microsoft.AspNetCore.Identity;

namespace OAuthHub.Models.Entities;

/// <summary>
/// Расширенный пользователь Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Отображаемое имя пользователя.</summary>
    public string? DisplayName { get; set; }

    /// <summary>URL аватарки (может приходить от провайдера).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Дата регистрации.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Навигационное свойство – сессии пользователя.</summary>
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}