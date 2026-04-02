namespace OAuthHub.Models.ViewModels;

public class ExportDataViewModel
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<string> LinkedProviders { get; set; } = new List<string>();
    public int TotalSessions { get; set; }
    public string? StatusMessage { get; set; }
}