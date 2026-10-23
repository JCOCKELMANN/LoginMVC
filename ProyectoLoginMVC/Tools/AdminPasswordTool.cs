using ProyectoLoginMVC.Services;

namespace ProyectoLoginMVC.Tools;

// Utilidad temporal. No escribe archivos ni se conecta a SQL Server.
internal static class AdminPasswordTool
{
    public static void Run(PasswordService service)
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Ejecuta esta utilidad en una terminal interactiva.");
            Environment.ExitCode = 1;
            return;
        }

        Console.Write("Contraseña para admin (no se mostrará): ");
        var password = ReadPassword();
        Console.Write("Repite la contraseña: ");
        var confirmation = ReadPassword();

        if (password != confirmation)
        {
            Console.Error.WriteLine("Las contraseñas no coinciden. Ejecuta de nuevo la utilidad.");
            Environment.ExitCode = 1;
            return;
        }

        string hash;
        try
        {
            hash = service.HashPassword(password);
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Introduce una contraseña no vacía de hasta 72 bytes UTF-8.");
            Environment.ExitCode = 1;
            return;
        }

        if (!service.VerifyPassword(confirmation, hash))
            throw new InvalidOperationException("No se pudo verificar el hash generado.");

        Console.WriteLine($"\nHash BCrypt: {hash}");
        Console.WriteLine("Verificación de la contraseña: correcta.");
        Console.WriteLine("\nCopia y ejecuta este INSERT en SQL Server:");
        Console.WriteLine($"""
            USE DB_LoginMVC;
            GO
            INSERT INTO dbo.Usuarios
                (Nombre, Apellido, Username, PasswordHash, IdRol, IdPuesto, NivelAcceso, Activo)
            VALUES
                ('Administrador', 'Sistema', 'admin', '{hash}',
                 (SELECT IdRol FROM dbo.Roles WHERE NombreRol = 'Administrador'), NULL, 10, 1);
            GO
            """);

        Console.Write("\nIntroduce otra contraseña para probar el mismo hash (puede ser incorrecta): ");
        Console.WriteLine(service.VerifyPassword(ReadPassword(), hash)
            ? "Resultado: coincide."
            : "Resultado: no coincide.");
    }

    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }
}
