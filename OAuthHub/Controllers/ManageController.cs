using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuthHub.Models.Entities;
using OAuthHub.Models.ViewModels;
using OAuthHub.Services;
using System.Security.Claims;

namespace OAuthHub.Controllers;

[Authorize]
public class ManageController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISessionService _sessionService;
    private readonly ILogger<ManageController> _logger;

    public ManageController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISessionService sessionService,
        ILogger<ManageController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var logins = await _userManager.GetLoginsAsync(user);
        var sessions = await _sessionService.GetUserSessionsAsync(user.Id);

        var model = new ManageViewModel
        {
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            HasPassword = await _userManager.HasPasswordAsync(user),
            TwoFactorEnabled = user.TwoFactorEnabled,
            ExternalLoginsCount = logins.Count,
            ActiveSessionsCount = sessions.Count(s => s.IsActive)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var hasPassword = await _userManager.HasPasswordAsync(user);
        return View(new ChangePasswordViewModel { HasPassword = hasPassword });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.HasPassword = await _userManager.HasPasswordAsync(user);

        if (!ModelState.IsValid) return View(model);

        IdentityResult result;

        if (model.HasPassword)
        {
            if (string.IsNullOrEmpty(model.OldPassword))
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Введите текущий пароль");
                return View(model);
            }
            result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, model.NewPassword);
        }

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Пароль успешно изменён!";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLogins()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var currentLogins = await _userManager.GetLoginsAsync(user);
        var allSchemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        var otherLogins = allSchemes.Where(s => !currentLogins.Any(l => l.LoginProvider == s.Name)).ToList();

        var hasPassword = await _userManager.HasPasswordAsync(user);

        var model = new ExternalLoginsViewModel
        {
            CurrentLogins = currentLogins,
            OtherLogins = otherLogins,
            ShowRemoveButton = hasPassword || currentLogins.Count > 1,
            StatusMessage = TempData["Success"]?.ToString() ?? TempData["Error"]?.ToString()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LinkLogin(string provider)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public IActionResult RemoveLogin(string loginProvider, string providerKey)
    {
        var model = new ConfirmActionViewModel
        {
            Title = "Отвязка внешнего аккаунта",
            Description = $"Вы уверены, что хотите отвязать аккаунт {loginProvider}?",
            ConfirmUrl = Url.Action("RemoveLoginConfirmed")!,
            CancelUrl = Url.Action("ExternalLogins")!,
            ConfirmButtonText = "Отвязать",
            ConfirmButtonCssClass = "btn-warning",
            HiddenFields = new Dictionary<string, string> { { "LoginProvider", loginProvider }, { "ProviderKey", providerKey } }
        };

        return View("ConfirmAction", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLoginConfirmed(string loginProvider, string providerKey)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);

        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = $"Аккаунт {loginProvider} успешно отвязан.";
        }
        else TempData["Error"] = "Не удалось отвязать аккаунт.";

        return RedirectToAction("ExternalLogins");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SyncProfileData(string provider)
    {
        var redirectUrl = Url.Action("SyncProfileDataCallback", "Manage", new { provider });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> SyncProfileDataCallback(string provider)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Не удалось получить свежие данные от провайдера.";
            return RedirectToAction("ExternalLogins");
        }

        var nameClaim = info.Principal.FindFirstValue(ClaimTypes.Name);
        var pictureClaim = info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:yandex:avatar_url")
                        ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:mailru:image")
                        ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "picture");

        bool isUpdated = false;

        if (!string.IsNullOrEmpty(nameClaim) && user.DisplayName != nameClaim)
        {
            user.DisplayName = nameClaim;
            isUpdated = true;
        }

        if (pictureClaim != null && user.AvatarUrl != pictureClaim.Value)
        {
            user.AvatarUrl = pictureClaim.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            await _userManager.UpdateAsync(user);
            TempData["Success"] = $"Данные профиля успешно синхронизированы с {provider}!";
        }
        else TempData["Success"] = $"Данные профиля уже актуальны.";

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> TwoFactorAuthentication()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var model = new Enable2faViewModel();

        if (!user.TwoFactorEnabled)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }
            model.SharedKey = unformattedKey;
        }

        ViewBag.TwoFactorEnabled = user.TwoFactorEnabled;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(Enable2faViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            ViewBag.TwoFactorEnabled = false;
            model.SharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            return View("TwoFactorAuthentication", model);
        }

        var verificationCode = model.Code.Replace(" ", "").Replace("-", "");

        var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!isCodeValid)
        {
            ModelState.AddModelError("Code", "Неверный проверочный код. Попробуйте снова.");
            ViewBag.TwoFactorEnabled = false;
            model.SharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            return View("TwoFactorAuthentication", model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        _logger.LogInformation("Пользователь {UserId} включил 2FA.", user.Id);

        TempData["Success"] = "Двухфакторная аутентификация успешно активирована!";
        return RedirectToAction("TwoFactorAuthentication");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        _logger.LogInformation("Пользователь {UserId} отключил 2FA.", user.Id);

        TempData["Success"] = "Двухфакторная аутентификация отключена.";
        return RedirectToAction("TwoFactorAuthentication");
    }

    [HttpGet]
    public IActionResult DeleteAccount()
    {
        var model = new ConfirmActionViewModel
        {
            Title = "Удаление аккаунта",
            Description = "Вы уверены, что хотите удалить свой аккаунт? Действие необратимо.",
            ConfirmUrl = Url.Action("DeleteAccountConfirmed")!,
            CancelUrl = Url.Action("Index")!,
            ConfirmButtonText = "Удалить аккаунт",
            ConfirmButtonCssClass = "btn-danger"
        };

        return View("ConfirmAction", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccountConfirmed()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        await _sessionService.TerminateAllSessionsExceptAsync(user.Id);
        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Аккаунт был удалён.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Error"] = "Произошла ошибка при удалении.";
        return RedirectToAction("Index");
    }
}