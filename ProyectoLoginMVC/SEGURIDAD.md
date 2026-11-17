# Seguridad del sitio

## Contraseñas

`PasswordService` genera y verifica hashes con BCrypt.Net-Next. El login compara
la contraseña ingresada con el hash guardado, sin almacenar contraseñas en texto
plano. BCrypt admite hasta 72 bytes UTF-8.

La creación de usuarios exige al menos 8 caracteres, una mayúscula y un símbolo.
Estas reglas se validan en el formulario y en el servidor. Las contraseñas ya
existentes conservan su validez.

## Acceso y sesiones

La autenticación usa cookies protegidas, HttpOnly y SameSite=Lax, con vencimiento
fijo de 30 minutos. En producción requieren HTTPS. El identificador de la cuenta
es un GUID. En cada solicitud autenticada se verifica que la cuenta exista,
esté activa y conserve su rol; en caso contrario se rechaza la sesión.

Solo el rol Administrador puede crear, consultar y eliminar usuarios. Las cuentas
nuevas reciben el rol Usuario y nivel de acceso 1. El puesto se consulta y valida
contra el catálogo de la base de datos. Las cuentas administrativas no se pueden
eliminar desde el sitio. El borrado requiere confirmación y un POST con token
antifalsificación, al igual que los demás formularios que modifican datos.

## Conexión y verificación

La conexión está en `ConnectionStrings:LoginConnection` de `appsettings.json`.
También puede configurarse con la variable de entorno
`ConnectionStrings__LoginConnection`. No guardes credenciales reales en Git.

Para compilar, ejecuta `dotnet build`. Con el sitio iniciado en desarrollo,
`Scripts/Test-Login.ps1` verifica los flujos de autenticación, permisos, puestos,
contraseñas y eliminación usando cuentas temporales que elimina al finalizar.
La prueba requiere sqlcmd; el funcionamiento del sitio usa Entity Framework Core
para acceder a SQL Server y no requiere esa herramienta.
