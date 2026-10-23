# Verificación de PasswordService

El servicio usa BCrypt.Net-Next 4.2.0 y está registrado como scoped en Program.cs.
HashPassword genera el salt automáticamente. VerifyPassword recibe la contraseña
original y el PasswordHash almacenado; nunca se comparan dos hashes nuevos.
Referencia de la biblioteca: https://github.com/BcryptNet/bcrypt.net

## Comprobar el administrador existente

Desde una terminal interactiva en la carpeta del proyecto:

```powershell
dotnet run --no-launch-profile -- --verificar-admin
```

1. Acepta DESKTOP-T9VI6PL o escribe tu servidor/instancia.
2. Introduce sa para autenticación SQL o deja vacío para Windows.
3. Si elegiste autenticación SQL, introduce su contraseña con entrada oculta.
4. Tras consultar el hash, introduce la contraseña original del usuario admin,
   también con entrada oculta. Se muestra únicamente si coincide.
5. Repite con una contraseña incorrecta: debe indicar contraseña incorrecta.

Requiere sqlcmd instalado y disponible en PATH. La utilidad únicamente ejecuta:

```sql
SELECT CONVERT(VARCHAR(MAX), CONVERT(VARBINARY(MAX), PasswordHash), 2)
FROM dbo.Usuarios WHERE Username = 'admin';
```

El transporte hexadecimal permite detectar espacios adicionales sin recortar el
hash original. Se exigen 60 bytes, el prefijo BCrypt y un costo entre 04 y 31.
El resultado es únicamente `Contraseña correcta` o `Contraseña incorrecta`.
Las solicitudes de entrada y los errores de conexión o formato se envían a stderr;
un error de consulta o formato no se presenta como contraseña incorrecta.

La base seleccionada es DB_LoginMVC. No inserta ni modifica registros, no inicia
el servidor web y no imprime el hash. Las contraseñas solo se usan en memoria;
la contraseña SQL se entrega a sqlcmd mediante el entorno de ese proceso hijo,
no mediante argumentos ni archivos. No compartas contraseñas por chat ni las
incluyas en comandos, archivos o el repositorio.

Una coincidencia devuelve código de salida 0. Una contraseña incorrecta, un hash
no válido o un fallo de consulta devuelve código 1. Un fallo de conexión no es
una prueba de que la contraseña del administrador sea incorrecta.

## Integración posterior

El compañero puede recibir PasswordService mediante inyección de dependencias
y llamar a VerifyPassword(contraseñaIntroducida, usuario.PasswordHash).
HashPassword rechaza entradas vacías o mayores de 72 bytes UTF-8. VerifyPassword
rechaza entradas vacías, nulas, mayores de 72 bytes y hashes de formato inválido.
No se han agregado login, cookies, sesiones ni cambios de base de datos.

Para compilar: dotnet build.

## Retirar la comprobación temporal

Cuando se haya confirmado el registro real, elimina Tools/AdminPasswordTool.cs,
el using ProyectoLoginMVC.Tools y el bloque --verificar-admin de Program.cs.
Conserva Services/PasswordService.cs y su registro AddScoped. El generador de
INSERT anterior ya se retiró porque el administrador está registrado.
