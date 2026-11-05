using ProyectoLoginMVC.Services;
using ProyectoLoginMVC.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PasswordService>();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
