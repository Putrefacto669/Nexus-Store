using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VentasMVC.Models;

public enum EstadoProducto { Nuevo, Seminuevo, Usado }
public enum CategoriaProducto { Accesorio, Software, Juego, Consola, Hardware }

public class Producto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "La marca es obligatoria."), StringLength(60)]
    public string Marca { get; set; } = "";

    [EnumDataType(typeof(EstadoProducto))]
    public EstadoProducto Estado { get; set; }

    [EnumDataType(typeof(CategoriaProducto))]
    public CategoriaProducto Categoria { get; set; }

    [Range(typeof(decimal), "0.01", "99999999", ErrorMessage = "Ingresa un precio entre 0.01 y 99,999,999.")]
    public decimal Precio { get; set; }

    [Range(0, 1000000, ErrorMessage = "Ingresa un stock entre 0 y 1,000,000.")]
    public int Stock { get; set; }

    public bool Disponible { get; set; } = true;

    [BindNever]
    public string? Foto { get; set; }

    [JsonIgnore]
    public bool EnVenta => Disponible && Stock > 0;
}
