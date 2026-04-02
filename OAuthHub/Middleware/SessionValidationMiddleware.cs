using Microsoft.AspNetCore.Identity;
using OAuthHub.Models.Entities;
using OAuthHub.Services;

namespace OAuthHub.Middleware;

/// <summary>
/// На каждом запросе проверяет, что сессия пользователя ещё активна в БД.
/// Если сессия завершена (админом или самим пользователем с другого устройства) —
/// разлогинивает и перенаправляет на страницу входа.
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CookieName = "OAuthHub.SessionId";

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Пропускаем страницы, которые должны быть доступны всегда
            if (path.StartsWith("/account/") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            var sessionIdStr = context.Request.Cookies[CookieName];

            if (!string.IsNullOrEmpty(sessionIdStr) && int.TryParse(sessionIdStr, out int sessionId))
            {
                var sessionService = context.RequestServices.GetRequiredService<ISessionService>();
                var isActive = await sessionService.IsSessionActiveAsync(sessionId);

                if (!isActive)
                {
                    // Сессия завершена — разлогиниваем
                    var signInManager = context.RequestServices
                        .GetRequiredService<SignInManager<ApplicationUser>>();

                    await signInManager.SignOutAsync();
                    context.Response.Cookies.Delete(CookieName);
                    context.Response.Redirect("/Account/Login?terminated=1");
                    return;
                }
            }
        }

        await _next(context);
    }
}