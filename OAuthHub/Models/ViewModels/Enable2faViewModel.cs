using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class Enable2faViewModel
{
    [Required(ErrorMessage = "Введите код проверки")]
    [StringLength(7, ErrorMessage = "Код должен быть 6 символов.", MinimumLength = 6)]
    [Display(Name = "6-значный код проверки")]
    public string Code { get; set; } = null!;

    public string? SharedKey { get; set; } // Секретный ключ для ввода в приложении
}