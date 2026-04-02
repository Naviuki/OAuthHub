namespace OAuthHub.Models.ViewModels;

public class ConfirmActionViewModel
{
    public string Title { get; set; } = "Подтверждение";
    public string Description { get; set; } = "Вы уверены, что хотите выполнить это действие?";
    public string ConfirmUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = "/Manage";
    public string ConfirmButtonText { get; set; } = "Подтвердить";
    public string ConfirmButtonCssClass { get; set; } = "btn-danger";

    /// <summary>Дополнительные скрытые поля для формы.</summary>
    public Dictionary<string, string> HiddenFields { get; set; } = new();
}