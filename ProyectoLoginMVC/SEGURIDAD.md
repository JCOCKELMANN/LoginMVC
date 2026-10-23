# Contraseñas con BCrypt

El proyecto ya referencia BCrypt.Net-Next 4.2.0. `PasswordService` se registra
como servicio scoped en `Program.cs` y utiliza `BCrypt.Net.BCrypt.HashPassword`
y `BCrypt.Net.BCrypt.Verify`. La biblioteca genera el salt automáticamente.
Referencia: https://github.com/BcryptNet/bcrypt.net/tree/4.2.0

## Generar el hash de admin

Desde la carpeta que contiene `ProyectoLoginMVC.csproj`, en una terminal:

```powershell
dotnet run --no-launch-profile -- --generar-hash-admin
```

Introduce la contraseña y repítela. La entrada permanece oculta y solo se
mantiene en memoria durante la ejecución. No pases contraseñas como argumentos
ni las escribas en archivos. Se rechazan entradas vacías y mayores de 72 bytes
UTF-8 (las letras acentuadas pueden ocupar más de un byte).

La utilidad verifica el hash y muestra el INSERT completo con ese hash.
Copia desde `USE DB_LoginMVC;` hasta el último `GO` y ejecútalo en una ventana
de consulta de SQL Server Management Studio, conectada a tu instancia.
Debe existir el rol `Administrador` y no debe existir otro usuario `admin`.
Se utiliza `IdPuesto = NULL`, `NivelAcceso = 10` y `Activo = 1`, como en el
INSERT existente. No es necesario indicar `IdUsuario` ni `FechaRegistro`:
la tabla ya tiene valores predeterminados. La utilidad no ejecuta SQL.

Puedes comprobar el registro sin consultar el hash:

```sql
USE DB_LoginMVC;
SELECT Nombre, Apellido, Username, IdRol, IdPuesto, NivelAcceso, Activo
FROM dbo.Usuarios
WHERE Username = 'admin';
```

## Probar el servicio

Después de mostrar el INSERT, la utilidad solicita otra contraseña y muestra
si coincide con el mismo hash. Prueba primero la correcta; repite la ejecución
y prueba una incorrecta para obtener `no coincide`. Cada ejecución genera un
hash nuevo, aunque introduzcas la misma contraseña.

Desde código que reciba `PasswordService` por inyección de dependencias:

```csharp
string hash = passwordService.HashPassword(password);
bool coincide = passwordService.VerifyPassword(password, hash);
```

`password` debe proceder de una entrada en ejecución y `hash` puede proceder
de `PasswordHash` en la base de datos. Nunca compares dos hashes nuevos.

Para compilar: `dotnet build`. Para iniciar normalmente el sitio: `dotnet run`.

## Retirar la utilidad temporal

Elimina `Tools/AdminPasswordTool.cs`, el `using ProyectoLoginMVC.Tools` y el
bloque de `--generar-hash-admin` en `Program.cs`. Conserva `PasswordService`
y su registro en inyección de dependencias.
