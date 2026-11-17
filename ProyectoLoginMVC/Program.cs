using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProyectoLoginMVC.Models;
using ProyectoLoginMVC.Services;
using ProyectoLoginMVC.Tools;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<CookieAuthService>(); // J: Esto permite que el controlador del login reciba tu servicio 
// mediante su constructor. 
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

        // Ruta propuesta para el formulario de login.
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

// Utilidad temporal: se ejecuta desde la terminal, sin iniciar el sitio web.
if (args.Contains("--verificar-admin"))
{
    using var scope = app.Services.CreateScope();
    AdminPasswordTool.Run(scope.ServiceProvider.GetRequiredService<PasswordService>());
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
