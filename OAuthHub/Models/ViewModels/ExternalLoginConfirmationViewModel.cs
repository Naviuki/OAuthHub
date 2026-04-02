using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class ExternalLoginConfirmationViewModel
{
    [Required(ErrorMessage = "Введите Email")]
    [EmailAddress(ErrorMessage = "Неверный формат Email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите имя")]
    [StringLength(50, ErrorMessage = "Имя слишком длинное")]
    [Display(Name = "Отображаемое имя")]
    public string DisplayName { get; set; } = null!;
}