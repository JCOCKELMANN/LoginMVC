using System.Text;

namespace ProyectoLoginMVC.Services;

public class PasswordService
{
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        // BCrypt admite hasta 72 bytes; evitamos truncar la contraseña.
        if (Encoding.UTF8.GetByteCount(password) > 72)
            throw new ArgumentException("La contraseña debe tener como máximo 72 bytes UTF-8.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)
            || Encoding.UTF8.GetByteCount(password) > 72)
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
