using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OAuthHub.Data;
using OAuthHub.Models.Entities;
using OAuthHub.Models.ViewModels;
using OAuthHub.Services;

namespace OAuthHub.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISessionService _sessionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    private const string SessionCookieName = "OAuthHub.SessionId";

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISessionService sessionService,
        ApplicationDbContext context,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _sessionService = sessionService;
        _context = context;
        _logger = logger;
    }

    // ==================== Helpers ====================

    private void SetSessionCookie(int sessionId)
    {
        HttpContext.Response.Cookies.Append(SessionCookieName, sessionId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private void ClearSessionCookie()
    {
        HttpContext.Response.Cookies.Delete(SessionCookieName);
    }

    // ==================== Blocked ====================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Blocked()
    {
        return View();
    }

    // ==================== Login ====================

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null, int? terminated = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Manage");

        if (terminated == 1)
        {
            ViewBag.SessionTerminated = true;
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
            return View(model);

        var userToCheck = await _userManager.FindByEmailAsync(model.Email);
        if (userToCheck != null && await _userManager.IsLockedOutAsync(userToCheck))
        {
            return RedirectToAction("Blocked");
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var session = await _sessionService.CreateSessionAsync(user.Id, HttpContext);
                SetSessionCookie(session.Id);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Action = "Вход",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Details = "Успешный вход по паролю"
                });
                await _context.SaveChangesAsync();
            }

            return LocalRedirect(model.ReturnUrl ?? "/Manage");
        }

        if (result.RequiresTwoFactor)
            return RedirectToAction("LoginWith2fa", new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });

        if (result.IsLockedOut)
            return RedirectToAction("Blocked");

        ModelState.AddModelError(string.Empty, "Неверный Email или пароль.");
        return View(model);
    }

    // ==================== 2FA Login ====================

    [HttpGet]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToAction("Login");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginWith2faViewModel { RememberMe = rememberMe });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToAction("Login");

        var authenticatorCode = model.TwoFactorCode.Replace(" ", "").Replace("-", "");
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode, model.RememberMe, model.RememberMachine);

        if (result.Succeeded)
        {
            var session = await _sessionService.CreateSessionAsync(user.Id, HttpContext);
            SetSessionCookie(session.Id);
            return LocalRedirect(returnUrl ?? "/Manage");
        }

        if (result.IsLockedOut)
            return RedirectToAction("Blocked");

        ModelState.AddModelError(string.Empty, "Неверный код аутентификации.");
        return View(model);
    }

    // ==================== Register ====================

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Manage");

        var model = new RegisterViewModel
        {
            ReturnUrl = returnUrl,
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            var session = await _sessionService.CreateSessionAsync(user.Id, HttpContext);
            SetSessionCookie(session.Id);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "Регистрация",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            return LocalRedirect(model.ReturnUrl ?? "/Manage");
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    // ==================== Logout ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var currentSession = await _sessionService.FindCurrentSessionAsync(userId, HttpContext);
            if (currentSession != null)
                await _sessionService.TerminateSessionAsync(currentSession.Id, userId);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = user?.Email,
                Action = "Выход",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }

        await _signInManager.SignOutAsync();
        ClearSessionCookie();
        return RedirectToAction("Index", "Home");
    }

    // ==================== External Login ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/Manage");

        if (remoteError != null)
        {
            TempData["Error"] = $"Ошибка внешнего провайдера: {remoteError}";
            return RedirectToAction("Login");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Не удалось получить данные от внешнего провайдера.";
            return RedirectToAction("Login");
        }

        // Если пользователь уже авторизован — привязка аккаунта
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var addResult = await _userManager.AddLoginAsync(currentUser, info);
                if (addResult.Succeeded)
                {
                    var pictureClaim = info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:yandex:avatar_url")
                                    ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:mailru:image")
                                    ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "picture");

                    if (string.IsNullOrEmpty(currentUser.AvatarUrl) && pictureClaim != null)
                    {
                        currentUser.AvatarUrl = pictureClaim.Value;
                        await _userManager.UpdateAsync(currentUser);
                    }
                    TempData["Success"] = $"Аккаунт {info.LoginProvider} успешно привязан!";
                }
                else TempData["Error"] = "Не удалось привязать аккаунт.";

                return RedirectToAction("ExternalLogins", "Manage");
            }
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

        if (signInResult.Succeeded)
        {
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                var session = await _sessionService.CreateSessionAsync(user.Id, HttpContext);
                SetSessionCookie(session.Id);

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Action = "Вход OAuth",
                    Details = $"Через {info.LoginProvider}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _context.SaveChangesAsync();
            }

            return LocalRedirect(returnUrl);
        }

        if (signInResult.RequiresTwoFactor)
            return RedirectToAction("LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = false });

        if (signInResult.IsLockedOut)
            return RedirectToAction("Blocked");

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;

        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel
        {
            Email = email ?? "",
            DisplayName = name ?? ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Manage");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            TempData["Error"] = "Ошибка загрузки данных внешнего провайдера.";
            return RedirectToAction("Login");
        }

        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;
            return View(model);
        }

        var pictureClaim = info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:yandex:avatar_url")
                        ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "urn:mailru:image")
                        ?? info.Principal.Claims.FirstOrDefault(c => c.Type == "picture");

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            AvatarUrl = pictureClaim?.Value,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false);

            var session = await _sessionService.CreateSessionAsync(user.Id, HttpContext);
            SetSessionCookie(session.Id);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "Регистрация OAuth",
                Details = $"Через {info.LoginProvider}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;
        return View(model);
    }

    // ==================== Access Denied ====================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}