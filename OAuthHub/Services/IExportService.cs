namespace OAuthHub.Services;

public interface IExportService
{
    /// <summary>Экспортировать данные пользователя в JSON.</summary>
    Task<string> ExportUserDataAsJsonAsync(string userId);

    /// <summary>Экспортировать данные пользователя в CSV.</summary>
    Task<string> ExportUserDataAsCsvAsync(string userId);
}