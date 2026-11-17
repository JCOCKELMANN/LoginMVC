namespace ProyectoLoginMVC.Models;

public class UserListItem
{
    public Guid IdUsuario { get; set; }
    public string Nombre { get; set; } = "";
    public string Username { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Puesto { get; set; } = "";
    public bool Activo { get; set; }
}
