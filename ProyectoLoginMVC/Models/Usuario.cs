using System;
using System.Collections.Generic;

namespace ProyectoLoginMVC.Models;

public partial class Usuario
{
    public Guid IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int IdRol { get; set; }

    public int? IdPuesto { get; set; }

    public int NivelAcceso { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Puesto? IdPuestoNavigation { get; set; }

    public virtual Role IdRolNavigation { get; set; } = null!;
}
