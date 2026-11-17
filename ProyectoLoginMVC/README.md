# TechFlow · Login MVC

Aplicación ASP.NET Core MVC (.NET 10) con registro, inicio de sesión,
panel protegido y cierre de sesión. Conserva las vistas TechFlow.

## Ejecutar

1. Instala el SDK de .NET 10 y SQL Server.
2. Si no existe `DB_LoginMVC`, ejecuta `SQL/LoginMVC (1).sql` en SQL Server.
   No vuelvas a ejecutar ese script sobre una base existente.
3. Configura `ConnectionStrings:LoginConnection` en `appsettings.json` según
   tu instancia. La configuración local usa `localhost` y autenticación de Windows.
4. Ejecuta `dotnet run` y abre `http://localhost:5163`.
5. Inicia sesión con el administrador existente y usa **Crear usuario** en el panel.

El inicio de sesión siempre abre el dashboard con la información de la cuenta,
navegación y accesos según el rol. Después de crear un usuario, se vuelve al
dashboard con una confirmación.

Solo el rol `Administrador` puede abrir y enviar el formulario de creación de
usuarios. Los registros nuevos reciben el rol `Usuario` y nivel de acceso 1.
El formulario carga el catálogo de la tabla `Puestos`, exige seleccionar uno
y guarda su identificador en `Usuarios.IdPuesto`.
Las contraseñas nuevas exigen una mayúscula y un símbolo. Se guardan con BCrypt, con un mínimo de 8 caracteres y un
máximo de 72 bytes UTF-8. El administrador existente conserva su contraseña.
El administrador puede entrar a **Eliminar usuarios** desde el dashboard, revisar
la cuenta y confirmar su eliminación permanente. Las cuentas con rol
Administrador están protegidas. Las sesiones de una cuenta eliminada dejan de
ser válidas en la siguiente solicitud.
El script de inserción del administrador contiene un marcador de contraseña;
no sirve para autenticar hasta reemplazarlo por un hash BCrypt válido.

La recuperación de acceso muestra la indicación de contactar al administrador;
no se ha configurado un servicio de correo para restablecer contraseñas.

En desarrollo se permiten cookies de sesión por HTTP local. En producción las
cookies requieren HTTPS. Los formularios POST validan el token antifalsificación.

## Verificación

`dotnet build` debe terminar sin errores ni advertencias. Con el sitio iniciado
en modo desarrollo, ejecuta desde PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Scripts/Test-Login.ps1
```

La prueba comprueba registro, duplicados, credenciales incorrectas, cookies,
protección del panel, tokens antifalsificación, redirecciones y cierre de sesión.
Crea dos cuentas temporales (administrador y usuario) y las elimina al finalizar.
Comprueba que visitantes y usuarios normales no puedan crear cuentas, ni por
URL ni mediante POST. Requiere `sqlcmd` y permisos
de Windows sobre la instancia local. Puedes pasar `-SqlServer` y `-BaseUrl`
si cambiaste la instancia o el puerto.
