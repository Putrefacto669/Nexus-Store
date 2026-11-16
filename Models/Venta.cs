namespace VentasMVC.Models;
public class Venta
{
    public int Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public decimal? CordobasPorDolar { get; set; }
    public string Cliente { get; set; } = "Consumidor final";
    public List<DetalleVenta> Detalles { get; set; } = new();
    public decimal Total => Detalles.Sum(d => d.Subtotal);
}
public class DetalleVenta
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}
public class NuevaVentaViewModel
{
    public string? Cliente { get; set; }
    public List<LineaNuevaVenta> Lineas { get; set; } = new();
}
public class LineaNuevaVenta
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
}
