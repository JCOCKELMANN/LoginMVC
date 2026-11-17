param(
    [string]$BaseUrl = 'http://localhost:5163',
    [string]$SqlServer = 'localhost'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$client = New-Object System.Net.Http.HttpClient($handler)
$username = 'smoke_' + [Guid]::NewGuid().ToString('N')
$password = [Guid]::NewGuid().ToString('N') + '!aA1'
$adminUsername = 'smoke_admin_' + [Guid]::NewGuid().ToString('N')
$bcryptPath = Join-Path $env:USERPROFILE '.nuget\packages\bcrypt.net-next\4.2.0\lib\net462\BCrypt-Net-Next.dll'
Add-Type -Path $bcryptPath
$hash = [BCrypt.Net.BCrypt]::HashPassword($password)

function Assert($condition, [string]$message) {
    if (-not $condition) { throw $message }
    Write-Output "OK: $message"
}

function Get-Page([string]$path) {
    return $client.GetAsync($BaseUrl + $path).GetAwaiter().GetResult()
}

function Post-Form([string]$path, [hashtable]$fields) {
    $pairs = New-Object 'System.Collections.Generic.Dictionary[string,string]'
    foreach ($key in $fields.Keys) { $pairs.Add($key, [string]$fields[$key]) }
    $content = New-Object System.Net.Http.FormUrlEncodedContent($pairs)
    return $client.PostAsync($BaseUrl + $path, $content).GetAwaiter().GetResult()
}

function Get-Token([string]$path) {
    $page = Get-Page $path
    $html = $page.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $match = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) { throw "No se encontro token en $path" }
    return [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}

try {
    $response = Get-Page '/'
    Assert ($response.StatusCode -eq 200) 'La ruta inicial muestra el login'
    $response = Get-Page '/Home/Dashboard'
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/Login')) 'El panel exige autenticacion'
    $response = Get-Page '/css/techflow.css'
    Assert ($response.StatusCode -eq 200) 'El front carga sus estilos'
    $response = Get-Page '/Account/Register'
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/Login')) 'El registro exige autenticacion'
    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Register' @{ Nombre='Prueba'; Apellido='Temporal'; Username=$username; Password=$password; __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/Login')) 'Un visitante no puede crear usuarios por POST'

    # Cuenta administrativa temporal para probar permisos sin alterar al admin existente.
    & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -Q "INSERT INTO Usuarios (Nombre, Apellido, Username, PasswordHash, IdRol, NivelAcceso, Activo) VALUES ('Prueba', 'Admin', '$adminUsername', '$hash', (SELECT IdRol FROM Roles WHERE NombreRol = 'Administrador'), 10, 1)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar el administrador temporal' }
    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Login' @{ Username=$adminUsername; Password=$password; __RequestVerificationToken=$token; returnUrl='/Account/Register' }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString() -eq '/Home/Dashboard') 'El login lleva primero al dashboard aunque se haya solicitado el registro'
    $response = Get-Page '/Home/Dashboard'
    Assert ($response.Content.ReadAsStringAsync().Result.Contains('/Account/Register')) 'El administrador tiene la opcion Crear usuario'
    $response = Get-Page '/Users/List'
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains($adminUsername) -and $response.Content.ReadAsStringAsync().Result.Contains('Estado')) 'El administrador puede consultar la vista de usuarios y su estado'
    $response = Post-Form '/Account/Register' @{ Nombre='Prueba'; Apellido='Temporal'; Username=$username; Password=$password }
    Assert ($response.StatusCode -eq 400) 'El registro exige token incluso al administrador'

    $token = Get-Token '/Account/Register'
    $response = Get-Page '/Account/Register'
    $html = [System.Net.WebUtility]::HtmlDecode($response.Content.ReadAsStringAsync().Result)
    $puestos = & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -h -1 -W -Q 'SET NOCOUNT ON; SELECT NombrePuesto FROM Puestos ORDER BY IdPuesto'
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el catalogo' }
    foreach ($puesto in $puestos) {
        if ($puesto.Trim()) { Assert ($html.Contains($puesto.Trim())) "El selector incluye: $($puesto.Trim())" }
    }
    $puestoId = [regex]::Match($html, '<option value="(\d+)"').Groups[1].Value
    Assert (-not [string]::IsNullOrEmpty($puestoId)) 'El selector contiene identificadores de la base'
    $fields = @{ Nombre='Prueba'; Apellido='Temporal'; Username=$username; Password=$password; __RequestVerificationToken=$token }
    $response = Post-Form '/Account/Register' $fields
    Assert ($response.StatusCode -eq 200 -and [System.Net.WebUtility]::HtmlDecode($response.Content.ReadAsStringAsync().Result).Contains('Selecciona un puesto.')) 'El puesto es obligatorio'
    $fields.IdPuesto = '-1'
    $response = Post-Form '/Account/Register' $fields
    Assert ($response.StatusCode -eq 200 -and [System.Net.WebUtility]::HtmlDecode($response.Content.ReadAsStringAsync().Result).Contains('Selecciona un puesto v')) 'Se rechazan puestos inexistentes'
    $fields.IdPuesto = $puestoId
    foreach ($weakPassword in @('abcdefg!', 'Abcdefgh', 'Ab!', 'Abcdefg ')) {
        $fields.Password = $weakPassword
        $response = Post-Form '/Account/Register' $fields
        Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains('validation-summary-errors')) 'El servidor rechaza una clave que no cumple la politica'
    }
    $fields.Password = $password
    $response = Post-Form '/Account/Register' $fields
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString() -eq '/Home/Dashboard') 'El registro guarda la cuenta y vuelve al dashboard'
    & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -Q "IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Username = '$username' AND IdPuesto = $puestoId) THROW 50001, 'No se guardo el puesto', 1;" | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'El puesto seleccionado se guarda en Usuarios.IdPuesto'
    $response = Get-Page '/Home/Dashboard'
    Assert ($response.Content.ReadAsStringAsync().Result.Contains('role="status"')) 'El dashboard muestra la confirmacion de creacion'
    $token = Get-Token '/Account/Register'
    $fields.__RequestVerificationToken = $token
    $response = Post-Form '/Account/Register' $fields
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains('ya est')) 'Se rechazan usuarios duplicados'

    $token = Get-Token '/Home/Dashboard'
    $response = Post-Form '/Account/Logout' @{ __RequestVerificationToken=$token }

    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Login' @{ Username=$username; Password='incorrecta'; __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains('incorrectos')) 'Se rechaza una clave incorrecta'
    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Login' @{ Username=$username; Password=$password; __RequestVerificationToken=$token; returnUrl='https://example.com' }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString() -eq '/Home/Dashboard') 'El login funciona y rechaza redirecciones externas'
    $response = Get-Page '/Home/Dashboard'
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains('Prueba Temporal')) 'La cookie permite entrar al panel'
    Assert (-not $response.Content.ReadAsStringAsync().Result.Contains('/Account/Register')) 'El usuario normal no tiene la opcion Crear usuario'
    $response = Get-Page '/Users/List'
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'La vista de usuarios esta restringida al administrador'
    $userId = (& sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -h -1 -W -Q "SET NOCOUNT ON; SELECT IdUsuario FROM Usuarios WHERE Username = '$username'").Trim()
    $adminId = (& sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -h -1 -W -Q "SET NOCOUNT ON; SELECT IdUsuario FROM Usuarios WHERE Username = '$adminUsername'").Trim()
    $response = Get-Page '/Users/Index'
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'El usuario normal no puede listar usuarios'
    $token = Get-Token '/Home/Dashboard'
    $response = Get-Page '/Account/Register'
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'El usuario normal no puede abrir el registro'
    $response = Post-Form '/Account/Register' @{ Nombre='Prueba'; Apellido='Temporal'; Username=$username; Password=$password; __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'El usuario normal no puede crear usuarios por POST'
    $response = Get-Page '/Account/AccessDenied'
    Assert ($response.StatusCode -eq 403) 'El acceso denegado muestra un error 403'
    $response = Post-Form "/Users/Delete/$userId" @{ __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'El usuario normal no puede eliminar cuentas'
    $response = Post-Form '/Account/Logout' @{ __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302) 'El cierre de sesion funciona'
    $response = Get-Page '/Home/Dashboard'
    Assert ($response.StatusCode -eq 302) 'El panel vuelve a exigir autenticacion'

    # Conserva una sesion normal activa en otro cliente para verificar su revocacion.
    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Login' @{ Username=$username; Password=$password; __RequestVerificationToken=$token }
    $normalHandler = New-Object System.Net.Http.HttpClientHandler
    $normalHandler.AllowAutoRedirect = $false
    foreach ($cookie in $handler.CookieContainer.GetCookies([Uri]$BaseUrl)) { $normalHandler.CookieContainer.Add($cookie) }
    $normalClient = New-Object System.Net.Http.HttpClient($normalHandler)
    $token = Get-Token '/Home/Dashboard'
    $response = Post-Form '/Account/Logout' @{ __RequestVerificationToken=$token }
    $token = Get-Token '/Account/Login'
    $response = Post-Form '/Account/Login' @{ Username=$adminUsername; Password=$password; __RequestVerificationToken=$token }
    $response = Get-Page '/Users/Index'
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains($username)) 'El administrador puede consultar los usuarios'
    $token = Get-Token '/Home/Dashboard'
    $response = Post-Form "/Users/Delete/$adminId" @{ __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/AccessDenied')) 'Las cuentas administrativas estan protegidas en el servidor'
    $response = Get-Page "/Users/Delete/$userId"
    Assert ($response.StatusCode -eq 200 -and $response.Content.ReadAsStringAsync().Result.Contains($username)) 'La eliminacion muestra una confirmacion de la cuenta'
    & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -Q "IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Username = '$username') THROW 50001, 'GET elimino el usuario', 1;" | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'Abrir la confirmacion no elimina al usuario'
    $response = Post-Form "/Users/Delete/$userId" @{}
    Assert ($response.StatusCode -eq 400) 'Eliminar exige token antifalsificacion'
    $token = Get-Token "/Users/Delete/$userId"
    $response = Post-Form "/Users/Delete/$userId" @{ __RequestVerificationToken=$token }
    Assert ($response.StatusCode -eq 302) 'El administrador puede confirmar la eliminacion'
    & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -Q "IF EXISTS (SELECT 1 FROM Usuarios WHERE Username = '$username') THROW 50001, 'El usuario sigue en la base', 1;" | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'La cuenta se elimina de la base de datos'
    $response = $normalClient.GetAsync($BaseUrl + '/Home/Dashboard').GetAwaiter().GetResult()
    Assert ($response.StatusCode -eq 302 -and $response.Headers.Location.ToString().Contains('/Account/Login')) 'Se invalida una sesion activa del usuario eliminado'
}
finally {
    if ($normalClient) { $normalClient.Dispose() }
    $client.Dispose()
    # Solo elimina la cuenta temporal cuyo nombre aleatorio genero esta prueba.
    & sqlcmd -S $SqlServer -E -C -d DB_LoginMVC -b -Q "DELETE FROM Usuarios WHERE Username IN ('$username', '$adminUsername')" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudo eliminar la cuenta temporal $username" }
}
