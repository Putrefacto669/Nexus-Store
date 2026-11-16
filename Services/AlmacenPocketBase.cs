using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VentasMVC.Models;

namespace VentasMVC.Services;

public sealed record FotoProducto(byte[] Contenido, string Extension);

public sealed class AlmacenPocketBase(HttpClient cliente, IWebHostEnvironment entorno, Monedas monedas)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task ImportarDatosAnteriores()
    {
        var estado = await Leer<EstadoAlmacen>("estado");
        if (estado.Importado) return;

        var carpeta = Path.Combine(entorno.ContentRootPath, "Data");
        var productos = await LeerArchivo<Producto>(Path.Combine(carpeta, "productos.json"));
        var ventas = await LeerArchivo<Venta>(Path.Combine(carpeta, "ventas.json"));

        using var formulario = Formulario(new { productos, ventas });
        foreach (var producto in productos.Where(p => p.Foto != null))
        {
            var nombre = producto.Foto!;
            var tipo = TipoFoto(nombre) ?? throw new InvalidDataException($"Foto inválida del producto #{producto.Id}.");
            var ruta = Path.Combine(carpeta, "fotos", nombre);
            AdjuntarFoto(formulario, "foto" + producto.Id, nombre, new StreamContent(File.OpenRead(ruta)), tipo);
        }

        using var respuesta = await cliente.PostAsync("importar", formulario);
        Comprobar(respuesta);
    }

    public Task<List<Producto>> Productos() => Leer<List<Producto>>("productos");
    public Task<List<Venta>> Ventas() => Leer<List<Venta>>("ventas");

    public async Task AgregarProducto(Producto producto, FotoProducto? foto)
    {
        var creado = await GuardarProducto(HttpMethod.Post, "productos", producto, foto)
            ?? throw new InvalidOperationException("No se encontró la ruta de productos en PocketBase.");
        producto.Id = creado.Id;
        producto.Foto = creado.Foto;
    }

    public async Task<bool> EditarProducto(Producto producto, FotoProducto? foto) =>
        await GuardarProducto(HttpMethod.Put, $"productos/{producto.Id}", producto, foto) != null;

    public async Task<bool> EliminarProducto(int id)
    {
        using var respuesta = await cliente.DeleteAsync($"productos/{id}");
        if (respuesta.StatusCode == HttpStatusCode.NotFound) return false;
        Comprobar(respuesta);
        return true;
    }

    public async Task<byte[]?> LeerFoto(string nombre)
    {
        using var respuesta = await cliente.GetAsync($"fotos/{Uri.EscapeDataString(nombre)}");
        if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;
        Comprobar(respuesta);
        return await respuesta.Content.ReadAsByteArrayAsync();
    }

    public async Task<(Venta? venta, string? error)> RegistrarVenta(NuevaVentaViewModel datos)
    {
        var clienteVenta = string.IsNullOrWhiteSpace(datos.Cliente) ? "Consumidor final" : datos.Cliente.Trim();
        using var respuesta = await cliente.PostAsJsonAsync("ventas", new
        {
            cliente = clienteVenta,
            lineas = datos.Lineas,
            cordobasPorDolar = monedas.CordobasPorDolar
        }, Json);
        var resultado = await LeerCuerpo<ResultadoVenta>(respuesta);
        return (resultado.Venta, resultado.Error);
    }

    public static string? TipoFoto(string nombre)
    {
        var extension = Path.GetExtension(nombre);
        if (extension.Length == 0 || !Guid.TryParseExact(nombre[..^extension.Length], "N", out _))
            return null;
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
    }

    private async Task<Producto?> GuardarProducto(HttpMethod metodo, string ruta, Producto producto, FotoProducto? foto)
    {
        var nombreFoto = foto == null ? null : Guid.NewGuid().ToString("N") + foto.Extension;
        using var formulario = Formulario(new
        {
            producto.Nombre, producto.Marca, producto.Estado, producto.Categoria,
            producto.Precio, producto.Stock, producto.Disponible, foto = nombreFoto
        });
        if (foto != null && nombreFoto != null)
            AdjuntarFoto(formulario, "foto", nombreFoto, new ByteArrayContent(foto.Contenido), TipoFoto(nombreFoto)!);

        using var peticion = new HttpRequestMessage(metodo, ruta) { Content = formulario };
        using var respuesta = await cliente.SendAsync(peticion);
        if (metodo == HttpMethod.Put && respuesta.StatusCode == HttpStatusCode.NotFound) return null;
        return await LeerCuerpo<Producto>(respuesta);
    }

    private async Task<T> Leer<T>(string ruta)
    {
        using var respuesta = await cliente.GetAsync(ruta);
        return await LeerCuerpo<T>(respuesta);
    }

    private static async Task<T> LeerCuerpo<T>(HttpResponseMessage respuesta)
    {
        Comprobar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(Json)
            ?? throw new InvalidDataException("PocketBase devolvió una respuesta vacía.");
    }

    private static void Comprobar(HttpResponseMessage respuesta)
    {
        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"PocketBase devolvió HTTP {(int)respuesta.StatusCode} en {respuesta.RequestMessage?.RequestUri?.AbsolutePath}.",
                null, respuesta.StatusCode);
    }

    private static MultipartFormDataContent Formulario<T>(T datos) =>
        new() { { new StringContent(JsonSerializer.Serialize(datos, Json), Encoding.UTF8), "datos" } };

    private static void AdjuntarFoto(MultipartFormDataContent formulario, string campo, string nombre,
        HttpContent contenido, string tipo)
    {
        contenido.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        formulario.Add(contenido, campo, nombre);
    }

    private static async Task<List<T>> LeerArchivo<T>(string archivo)
    {
        if (!File.Exists(archivo)) return [];
        await using var contenido = File.OpenRead(archivo);
        return await JsonSerializer.DeserializeAsync<List<T>>(contenido, Json) ?? [];
    }

    private sealed record EstadoAlmacen(bool Importado);
    private sealed record ResultadoVenta(Venta? Venta, string? Error);
}
