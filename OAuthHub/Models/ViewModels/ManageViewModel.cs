using System.ComponentModel.DataAnnotations;

namespace OAuthHub.Models.ViewModels;

public class ManageViewModel
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasPassword { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int ExternalLoginsCount { get; set; }
    public int ActiveSessionsCount { get; set; }
}

public class ChangePasswordViewModel
{
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string? OldPassword { get; set; }

    [Required(ErrorMessage = "Введите новый пароль")]
    [StringLength(100, ErrorMessage = "Пароль должен быть от {2} до {1} символов", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение нового пароля")]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmNewPassword { get; set; } = null!;

    /// <summary>Есть ли у пользователя уже установленный пароль.</summary>
    public bool HasPassword { get; set; }
}

public class ExternalLoginsViewModel
{
    public IList<Microsoft.AspNetCore.Identity.UserLoginInfo> CurrentLogins { get; set; } = new List<Microsoft.AspNetCore.Identity.UserLoginInfo>();
    public IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme> OtherLogins { get; set; } = new List<Microsoft.AspNetCore.Authentication.AuthenticationScheme>();
    public bool ShowRemoveButton { get; set; }
    public string? StatusMessage { get; set; }
}