using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoLoginMVC.Models;

namespace ProyectoLoginMVC.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class UsersController(LoginContext db, ILogger<UsersController> logger) : Controller
{
    private IQueryable<UserListItem> Listado() => db.Usuarios.AsNoTracking()
        .Select(u => new UserListItem
        {
            IdUsuario = u.IdUsuario, Nombre = u.Nombre + " " + u.Apellido,
            Username = u.Username, Rol = u.IdRolNavigation.NombreRol,
            Puesto = u.IdPuestoNavigation != null ? u.IdPuestoNavigation.NombrePuesto : "Sin puesto",
            Activo = u.Activo
        });

    [HttpGet]
    public Task<IActionResult> Index() => CargarListadoAsync("Index");

    [HttpGet]
    public Task<IActionResult> List() => CargarListadoAsync("List");

    private async Task<IActionResult> CargarListadoAsync(string vista)
    {
        try { return View(vista, await Listado().OrderBy(u => u.Username).ToListAsync()); }
        catch (SqlException ex)
        {
            logger.LogError(ex, "No se pudo cargar el listado de usuarios.");
            ModelState.AddModelError("", "No se pudieron cargar los usuarios. Intenta nuevamente.");
            return View(vista, new List<UserListItem>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var usuario = await Listado().SingleOrDefaultAsync(u => u.IdUsuario == id);
        if (usuario is null) return NotFound();
        if (usuario.Rol == "Administrador") return Forbid();
        return View(usuario);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var usuario = await db.Usuarios.Include(u => u.IdRolNavigation)
                .SingleOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario is null) return NotFound();
            if (usuario.IdRolNavigation.NombreRol == "Administrador") return Forbid();
            var deleted = await db.Usuarios.Where(u => u.IdUsuario == id &&
                u.IdRolNavigation.NombreRol != "Administrador").ExecuteDeleteAsync();
            if (deleted == 0) return NotFound();
            TempData["Success"] = "Usuario eliminado correctamente.";
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "No se pudo eliminar el usuario.");
            TempData["Error"] = "No se pudo eliminar el usuario. Puede tener información relacionada o existir un problema de conexión.";
        }
        return RedirectToAction(nameof(Index));
    }
}
