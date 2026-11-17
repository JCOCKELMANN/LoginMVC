using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProyectoLoginMVC.Models;

public class RegisterViewModel : LoginViewModel
{
    [Required(ErrorMessage = "Selecciona un puesto.")]
    public int? IdPuesto { get; set; }

    [BindNever]
    public List<SelectListItem> Puestos { get; set; } = [];

    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu apellido.")]
    [StringLength(100)]
    public string Apellido { get; set; } = "";
}
