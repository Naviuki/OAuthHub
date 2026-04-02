using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите Email")]
    [EmailAddress(ErrorMessage = "Неверный формат Email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = null!;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    /// <summary>Список доступных внешних провайдеров (заполняется контроллером).</summary>
    public IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { get; set; } = new List<Microsoft.AspNetCore.Authentication.AuthenticationScheme>();
}