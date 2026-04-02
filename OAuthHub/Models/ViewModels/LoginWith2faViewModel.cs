using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class LoginWith2faViewModel
{
    [Required(ErrorMessage = "Введите код")]
    [StringLength(7, ErrorMessage = "Код должен быть от {2} до {1} символов.", MinimumLength = 6)]
    [DataType(DataType.Text)]
    [Display(Name = "6-значный код из приложения")]
    public string TwoFactorCode { get; set; } = null!;

    [Display(Name = "Запомнить этот браузер")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }
}