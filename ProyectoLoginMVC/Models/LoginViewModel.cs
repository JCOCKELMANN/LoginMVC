using System.ComponentModel.DataAnnotations;

namespace ProyectoLoginMVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [StringLength(50)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
