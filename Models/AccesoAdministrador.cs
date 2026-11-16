using System.ComponentModel.DataAnnotations;

namespace VentasMVC.Models;

public sealed class AccesoAdministrador
{
    [Required(ErrorMessage = "Ingresa el usuario."), StringLength(64)]
    public string Usuario { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la contraseña."), StringLength(256), DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
