using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите имя")]
    [StringLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
    [Display(Name = "Отображаемое имя")]
    public string DisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Введите Email")]
    [EmailAddress(ErrorMessage = "Неверный формат Email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите пароль")]
    [StringLength(100, ErrorMessage = "Пароль должен быть от {2} до {1} символов", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = null!;

    public string? ReturnUrl { get; set; }

    public IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> ExternalLogins { get; set; } = new List<Microsoft.AspNetCore.Authentication.AuthenticationScheme>();
}