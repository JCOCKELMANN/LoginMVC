using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoLoginMVC.Models;
using ProyectoLoginMVC.Services;

namespace ProyectoLoginMVC.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class AccountController(LoginContext db, PasswordService passwords,
    CookieAuthService cookies, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);
        try
        {
            var usuario = await db.Usuarios.AsNoTracking().Include(u => u.IdRolNavigation)
                .SingleOrDefaultAsync(u => u.Username == model.Username.Trim());
            if (usuario is null || !usuario.Activo ||
                !passwords.VerifyPassword(model.Password, usuario.PasswordHash))
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            await cookies.IniciarSesionAsync(HttpContext, usuario.IdUsuario,
                $"{usuario.Nombre} {usuario.Apellido}", usuario.Username,
                usuario.IdRolNavigation.NombreRol);
            return RedirectToAction("Dashboard", "Home");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "No se pudo consultar la base de datos al iniciar sesión.");
            ModelState.AddModelError("", "No se pudo conectar con la base de datos. Intenta nuevamente.");
            return View(model);
        }
    }

    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Register()
    {
        var model = new RegisterViewModel();
        await CargarPuestosAsync(model);
        return View(model);
    }

    private async Task CargarPuestosAsync(RegisterViewModel model)
    {
        try
        {
            model.Puestos = await db.Puestos.AsNoTracking().OrderBy(p => p.NombrePuesto)
                .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = p.IdPuesto.ToString(), Text = p.NombrePuesto
                }).ToListAsync();
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "No se pudieron cargar los puestos.");
            ModelState.AddModelError("", "No se pudieron cargar los puestos. Revisa la conexión con la base de datos.");
        }
    }

    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        await CargarPuestosAsync(model);
        if (model.IdPuesto.HasValue &&
            !model.Puestos.Any(p => p.Value == model.IdPuesto.Value.ToString()))
            ModelState.AddModelError("IdPuesto", "Selecciona un puesto válido.");
        if (Encoding.UTF8.GetByteCount(model.Password ?? "") > 72 ||
            (model.Password?.Length ?? 0) < 8)
            ModelState.AddModelError("Password", "Usa al menos 8 caracteres y como máximo 72 bytes UTF-8.");
        if (!(model.Password ?? "").Any(char.IsUpper) ||
            !(model.Password ?? "").Any(c => char.IsPunctuation(c) || char.IsSymbol(c)))
            ModelState.AddModelError("Password", "La contraseña debe incluir al menos una mayúscula y un símbolo.");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var username = model.Username.Trim();
            if (await db.Usuarios.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("Username", "Este usuario ya está registrado.");
                return View(model);
            }
            var rol = await db.Roles.SingleOrDefaultAsync(r => r.NombreRol == "Usuario");
            if (rol is null)
            {
                ModelState.AddModelError("", "Falta configurar el rol Usuario en la base de datos.");
                return View(model);
            }
            db.Usuarios.Add(new Usuario
            {
                Nombre = model.Nombre.Trim(), Apellido = model.Apellido.Trim(),
                Username = username, PasswordHash = passwords.HashPassword(model.Password!),
                IdRol = rol.IdRol, IdPuesto = model.IdPuesto, NivelAcceso = 1, Activo = true
            });
            await db.SaveChangesAsync();
            TempData["Success"] = "Usuario creado correctamente.";
            return RedirectToAction("Dashboard", "Home");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError("Username", "Este usuario ya está registrado.");
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException)
        {
            logger.LogError(ex, "No se pudo registrar el usuario.");
            ModelState.AddModelError("", "No se pudo guardar la cuenta. Revisa la conexión con la base de datos.");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Recuperar() => View();

    [HttpGet, Authorize]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await cookies.CerrarSesionAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }
}
