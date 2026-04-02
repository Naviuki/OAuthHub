using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuthHub.Data;
using OAuthHub.Middleware;
using OAuthHub.Models.Entities;
using OAuthHub.Services;

var builder = WebApplication.CreateBuilder(args);

// === База данных ===
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(36500);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddRoles<IdentityRole>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

// === OAuth Провайдеры ===
var authSection = builder.Configuration.GetSection("Authentication");
var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrEmpty(authSection["Google:ClientId"]))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = authSection["Google:ClientId"]!;
        options.ClientSecret = authSection["Google:ClientSecret"]!;
        options.SaveTokens = true;
    });
}

if (!string.IsNullOrEmpty(authSection["Yandex:ClientId"]))
{
    authBuilder.AddYandex(options =>
    {
        options.ClientId = authSection["Yandex:ClientId"]!;
        options.ClientSecret = authSection["Yandex:ClientSecret"]!;
        options.SaveTokens = true;
    });
}

if (!string.IsNullOrEmpty(authSection["MailRu:ClientId"]))
{
    authBuilder.AddMailRu(options =>
    {
        options.ClientId = authSection["MailRu:ClientId"]!;
        options.ClientSecret = authSection["MailRu:ClientSecret"]!;
        options.SaveTokens = true;
    });
}

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IExportService, ExportService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 1. Проверка блокировки пользователя
app.UseMiddleware<BlockedUserMiddleware>();
// 2. Проверка активности сессии — РЕАЛЬНОЕ завершение сессий
app.UseMiddleware<SessionValidationMiddleware>();

// === Инициализация БД и администратора ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    var adminEmail = "admin@oauthhub.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = "Главный Администратор",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();