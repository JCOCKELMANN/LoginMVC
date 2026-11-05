using Microsoft.AspNetCore.Authentication.Cookies; //J: Componentes de autenticación por cookies
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // Ruta propuesta para el formulario de login.
        options.LoginPath = "/Account/Login";

        // El ticket de autenticación vence a los 30 minutos.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
    });



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
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
