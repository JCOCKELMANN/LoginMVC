using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProyectoLoginMVC.Models;
using ProyectoLoginMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios del sitio.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<CookieAuthService>();
// Autenticación mediante cookies.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LoginMVC.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        // Rutas de acceso.
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<LoginContext>();
            var validId = Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
            var rol = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!validId || !await db.Usuarios.AsNoTracking().AnyAsync(u =>
                u.IdUsuario == id && u.Activo && u.IdRolNavigation.NombreRol == rol))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };

        // El ticket de autenticación vence a los 30 minutos.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
    });

builder.Services.AddDbContext<LoginContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LoginConnection")));

var app = builder.Build();

// Configuración de las solicitudes HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
// Reconoce al usuario a partir de su cookie de autenticación.
app.UseAuthentication();
// Comprueba si el usuario puede acceder a la página solicitada.
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
