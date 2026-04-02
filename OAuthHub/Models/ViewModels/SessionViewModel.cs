namespace OAuthHub.Models.ViewModels;

public class SessionListViewModel
{
    public IList<SessionItemViewModel> Sessions { get; set; } = new List<SessionItemViewModel>();
    public int? CurrentSessionId { get; set; }
}

public class SessionItemViewModel
{
    public int Id { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
}