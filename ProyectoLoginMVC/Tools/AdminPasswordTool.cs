using System.Diagnostics;
using ProyectoLoginMVC.Services;

namespace ProyectoLoginMVC.Tools;

// Comprobación manual temporal de solo lectura con sqlcmd.
internal static class AdminPasswordTool
{
    public static void Run(PasswordService service)
    {
        if (Console.IsInputRedirected)
        {
            Fail("Ejecuta esta utilidad en una terminal interactiva.");
            return;
        }

        Console.Error.Write("Servidor SQL Server [DESKTOP-T9VI6PL]: ");
        var server = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(server))
            server = "DESKTOP-T9VI6PL";

        Console.Error.Write("Usuario SQL (Enter para autenticación de Windows): ");
        var sqlUser = Console.ReadLine();

        string hash;
        try
        {
            var startInfo = new ProcessStartInfo("sqlcmd")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in new[]
            {
                "-S", server, "-d", "DB_LoginMVC", "-b", "-l", "5", "-t", "10",
                "-h", "-1", "-W", "-Q",
                // El transporte hexadecimal conserva espacios y bytes del valor almacenado.
                "SET NOCOUNT ON; SELECT CONVERT(VARCHAR(MAX), CONVERT(VARBINARY(MAX), PasswordHash), 2) FROM dbo.Usuarios WHERE Username = 'admin';"
            })
                startInfo.ArgumentList.Add(argument);

            if (string.IsNullOrWhiteSpace(sqlUser))
                startInfo.ArgumentList.Add("-E");
            else
            {
                startInfo.ArgumentList.Add("-U");
                startInfo.ArgumentList.Add(sqlUser);
                Console.Error.Write("Contraseña de SQL Server (entrada oculta): ");
                // Solo en el entorno del proceso hijo; nunca como argumento ni en archivos.
                startInfo.Environment["SQLCMDPASSWORD"] = ReadPassword();
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar sqlcmd.");
            startInfo.Environment.Remove("SQLCMDPASSWORD");
            var output = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Fail("No se pudo consultar SQL Server. Comprueba la instancia, las credenciales y la conexión con SSMS.");
                return;
            }

            // Solo quitamos el formato de salida de sqlcmd, nunca espacios del hash.
            var hex = output.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(hex, @"\A[0-9A-Fa-f]{120}\z"))
            {
                Fail("No se encontró un único hash de 60 bytes para admin. Revisa el registro, su longitud y posibles espacios adicionales.");
                return;
            }

            hash = System.Text.Encoding.ASCII.GetString(Convert.FromHexString(hex));
            if (!System.Text.RegularExpressions.Regex.IsMatch(hash,
                @"\A\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[./A-Za-z0-9]{53}\z"))
            {
                Fail("El hash almacenado no tiene el formato BCrypt esperado (60 caracteres, prefijo y costo válidos).");
                return;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Fail("No se pudo ejecutar sqlcmd. Comprueba que esté instalado y disponible en PATH.");
            return;
        }

        Console.Error.Write("Contraseña que deseas verificar para admin (entrada oculta): ");
        var matches = service.VerifyPassword(ReadPassword(), hash);
        Console.WriteLine(matches ? "Contraseña correcta" : "Contraseña incorrecta");
        if (!matches)
            Environment.ExitCode = 1;
    }

    private static void Fail(string message)
    {
        Console.Error.WriteLine(message);
        Environment.ExitCode = 1;
    }

    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                return password.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
    }
}
