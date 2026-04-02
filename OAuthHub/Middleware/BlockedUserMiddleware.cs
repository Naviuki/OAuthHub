using Microsoft.AspNetCore.Identity;
using OAuthHub.Models.Entities;
using OAuthHub.Services;

namespace OAuthHub.Middleware;

public class BlockedUserMiddleware
{
    private readonly RequestDelegate _next;

    public BlockedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (path.StartsWith("/account/blocked") ||
                path.StartsWith("/account/logout") ||
                path.StartsWith("/account/accessdenied") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            var user = await userManager.GetUserAsync(context.User);

            if (user == null)
            {
                await signInManager.SignOutAsync();
                context.Response.Cookies.Delete("OAuthHub.SessionId");
                context.Response.Redirect("/Account/Login");
                return;
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                var sessionService = context.RequestServices.GetRequiredService<ISessionService>();
                await sessionService.TerminateAllSessionsExceptAsync(user.Id);

                await signInManager.SignOutAsync();
                context.Response.Cookies.Delete("OAuthHub.SessionId");
                context.Response.Redirect("/Account/Blocked");
                return;
            }
        }

        await _next(context);
    }
}