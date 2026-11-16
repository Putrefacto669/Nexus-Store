using System.Text.Json;
using VentasMVC.Models;

namespace VentasMVC.Services;

public sealed record FotoProducto(byte[] Contenido, string Extension);

// Almacén sencillo para una sola instancia de la aplicación, sin base de datos.
public sealed class AlmacenJson
{
    public string DirectorioFotos { get; }

    private readonly Monedas _monedas;
    private readonly ILogger<AlmacenJson> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _productosArchivo;
    private readonly string _ventasArchivo;
    private readonly JsonSerializerOptions _opciones = new() { WriteIndented = true };

    public AlmacenJson(IWebHostEnvironment environment, Monedas monedas, ILogger<AlmacenJson> logger)
    {
        var directorio = Path.Combine(environment.ContentRootPath, "Data");
        _monedas = monedas;
        _logger = logger;
        DirectorioFotos = Path.Combine(directorio, "fotos");
        Directory.CreateDirectory(DirectorioFotos);
        _productosArchivo = Path.Combine(directorio, "productos.json");
        _ventasArchivo = Path.Combine(directorio, "ventas.json");
        if (!File.Exists(_productosArchivo))
            File.WriteAllText(_productosArchivo, JsonSerializer.Serialize(new List<Producto>
            {
                new() { Id = 1, Nombre = "Mouse inalámbrico", Precio = 350, Stock = 20 },
                new() { Id = 2, Nombre = "Teclado USB", Precio = 520, Stock = 15 },
                new() { Id = 3, Nombre = "Memoria USB 32 GB", Precio = 280, Stock = 30 }
            }, _opciones));
        if (!File.Exists(_ventasArchivo)) File.WriteAllText(_ventasArchivo, "[]");
    }

    private async Task<List<T>> Leer<T>(string archivo) =>
        JsonSerializer.Deserialize<List<T>>(await File.ReadAllTextAsync(archivo), _opciones) ?? new();

    private async Task Guardar<T>(string archivo, List<T> datos)
    {
        var temporal = archivo + ".tmp";
        await File.WriteAllTextAsync(temporal, JsonSerializer.Serialize(datos, _opciones));
        File.Move(temporal, archivo, overwrite: true);
    }

    public async Task<List<Producto>> Productos()
    {
        await _mutex.WaitAsync();
        try { return await Leer<Producto>(_productosArchivo); }
        finally { _mutex.Release(); }
    }
    public async Task<List<Venta>> Ventas()
    {
        await _mutex.WaitAsync();
        try { return await Leer<Venta>(_ventasArchivo); }
        finally { _mutex.Release(); }
    }
    public async Task AgregarProducto(Producto producto, FotoProducto? foto)
    {
        await _mutex.WaitAsync();
        try
        {
            var productos = await Leer<Producto>(_productosArchivo);
            producto.Id = productos.Count == 0 ? 1 : productos.Max(p => p.Id) + 1;
            productos.Add(producto);
            await GuardarProducto(productos, producto, foto);
        }
        finally { _mutex.Release(); }
    }
    public async Task<bool> EditarProducto(Producto modificado, FotoProducto? foto)
    {
        await _mutex.WaitAsync();
        try
        {
            var productos = await Leer<Producto>(_productosArchivo);
            var producto = productos.FirstOrDefault(p => p.Id == modificado.Id);
            if (producto == null) return false;
            producto.Nombre = modificado.Nombre;
            producto.Precio = modificado.Precio;
            producto.Stock = modificado.Stock;
            producto.Marca = modificado.Marca;
            producto.Estado = modificado.Estado;
            producto.Categoria = modificado.Categoria;
            producto.Disponible = modificado.Disponible;
            await GuardarProducto(productos, producto, foto);
            return true;
        }
        finally { _mutex.Release(); }
    }
    private async Task GuardarProducto(List<Producto> productos, Producto producto, FotoProducto? foto)
    {
        if (foto == null)
        {
            await Guardar(_productosArchivo, productos);
            return;
        }

        var anterior = producto.Foto;
        var nombre = Guid.NewGuid().ToString("N") + foto.Extension;
        producto.Foto = nombre;
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(DirectorioFotos, nombre), foto.Contenido);
            await Guardar(_productosArchivo, productos);
        }
        catch
        {
            BorrarFoto(nombre);
            throw;
        }
        BorrarFoto(anterior);
    }

    private void BorrarFoto(string? nombre)
    {
        if (nombre == null) return;
        try { File.Delete(Path.Combine(DirectorioFotos, nombre)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "No se pudo borrar la foto {Foto}.", nombre);
        }
    }

    public async Task<bool> EliminarProducto(int id)
    {
        await _mutex.WaitAsync();
        try
        {
            var productos = await Leer<Producto>(_productosArchivo);
            var producto = productos.FirstOrDefault(p => p.Id == id);
            if (producto == null) return false;
            productos.Remove(producto);
            await Guardar(_productosArchivo, productos);
            BorrarFoto(producto.Foto);
            return true;
        }
        finally { _mutex.Release(); }
    }
    public async Task<(Venta? venta, string? error)> RegistrarVenta(NuevaVentaViewModel datos)
    {
        await _mutex.WaitAsync();
        try
        {
            if (datos.Lineas == null || datos.Lineas.Count == 0)
                return (null, "Agregá al menos un producto.");
            var productos = await Leer<Producto>(_productosArchivo);
            if (datos.Lineas.Any(x => x.Cantidad <= 0))
                return (null, "La cantidad debe ser mayor que cero.");
            var cantidades = datos.Lineas.GroupBy(x => x.ProductoId)
                .ToDictionary(g => g.Key, g => g.Sum(x => (long)x.Cantidad));
            var detalles = new List<DetalleVenta>();
            foreach (var linea in cantidades)
            {
                var producto = productos.FirstOrDefault(p => p.Id == linea.Key);
                if (producto == null) return (null, "Un producto ya no existe.");
                if (!producto.Disponible) return (null, $"El producto {producto.Nombre} no está disponible.");
                if (linea.Value > int.MaxValue)
                    return (null, "La cantidad debe ser mayor que cero.");
                if (linea.Value > producto.Stock)
                    return (null, $"Stock insuficiente para {producto.Nombre} (disponible: {producto.Stock}).");
                detalles.Add(new DetalleVenta { ProductoId = producto.Id, ProductoNombre = producto.Nombre,
                    Cantidad = (int)linea.Value, PrecioUnitario = producto.Precio });
            }
            var ventas = await Leer<Venta>(_ventasArchivo);
            var venta = new Venta { Id = ventas.Count == 0 ? 1 : ventas.Max(v => v.Id) + 1,
                FechaUtc = DateTime.UtcNow,
                CordobasPorDolar = _monedas.CordobasPorDolar,
                Cliente = string.IsNullOrWhiteSpace(datos.Cliente) ? "Consumidor final" : datos.Cliente.Trim(),
                Detalles = detalles };
            foreach (var detalle in detalles)
                productos.First(p => p.Id == detalle.ProductoId).Stock -= detalle.Cantidad;
            // En esta versión didáctica se guardan dos archivos consecutivamente; no son una transacción ACID.
            await Guardar(_productosArchivo, productos);
            ventas.Add(venta);
            await Guardar(_ventasArchivo, ventas);
            return (venta, null);
        }
        finally { _mutex.Release(); }
    }
}
