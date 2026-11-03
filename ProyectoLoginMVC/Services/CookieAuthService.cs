using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace ProyectoLoginMVC.Services;

public class CookieAuthService
{
    // Llamar únicamente después de validar las credenciales.
    // Los datos deben provenir del usuario consultado en SQL, no directamente del formulario.
    public Task IniciarSesionAsync(
        HttpContext httpContext,
        int identificador,
        string nombre,
        string usuario)
    {
        ArgumentNullException.ThrowIfNull(httpContext );
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);

        // Claims: datos de identidad incluidos en el ticket protegido.
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                identificador.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, nombre),
            new Claim("Usuario", usuario)
        };

        var identidad = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identidad);

        var propiedades = new AuthenticationProperties
        {
            // No activar "recordarme" ni persistencia entre sesiones.
            IsPersistent = false
        };

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            propiedades);
    }

    public Task CerrarSesionAsync(HttpContext httpContext )
    {
        ArgumentNullException.ThrowIfNull(httpContext );

        return httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
