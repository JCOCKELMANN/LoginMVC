using System;
using System.Collections.Generic;

namespace ProyectoLoginMVC.Models;

public partial class Puesto
{
    public int IdPuesto { get; set; }

    public string NombrePuesto { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
