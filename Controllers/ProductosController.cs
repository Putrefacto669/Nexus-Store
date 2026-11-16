using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentasMVC.Models;
using VentasMVC.Services;

namespace VentasMVC.Controllers;

[Authorize(Roles = "Administrador")]
public class ProductosController(AlmacenPocketBase almacen) : Controller
{
    private const int MaximoFoto = 3 * 1024 * 1024;

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Index() => View(new CatalogoViewModel(await almacen.Productos()));

    [AllowAnonymous, HttpGet("catalogo/{marca}")]
    public async Task<IActionResult> Marca(string marca)
    {
        var tienda = MarcaTienda.Todas.FirstOrDefault(m => string.Equals(m.Id, marca, StringComparison.OrdinalIgnoreCase));
        if (tienda == null) return NotFound();
        var productos = (await almacen.Productos())
            .Where(p => string.Equals(p.Marca, tienda.Nombre, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return View("Index", new CatalogoViewModel(productos, tienda));
    }

    [AllowAnonymous, HttpGet("fotos/productos/{nombre}")]
    public async Task<IActionResult> Foto(string nombre)
    {
        var tipo = AlmacenPocketBase.TipoFoto(nombre);
        if (tipo == null) return NotFound();

        var foto = await almacen.LeerFoto(nombre);
        if (foto == null) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(foto, tipo);
    }

    [HttpGet]
    public async Task<IActionResult> Administrar() => View(await almacen.Productos());

    [HttpGet]
    public IActionResult Crear() => View(new Producto());

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaximoFoto + 65536)]
    public async Task<IActionResult> Crear(Producto producto, IFormFile? fotoProducto)
    {
        Normalizar(producto);
        if (fotoProducto == null)
            ModelState.AddModelError("fotoProducto", "Selecciona una foto del producto.");
        if (!ModelState.IsValid) return View(producto);

        var foto = await LeerFoto(fotoProducto);
        if (!ModelState.IsValid) return View(producto);

        await almacen.AgregarProducto(producto, foto);
        TempData["Ok"] = "Producto agregado correctamente.";
        return RedirectToAction(nameof(Administrar));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var producto = (await almacen.Productos()).FirstOrDefault(p => p.Id == id);
        return producto == null ? NotFound() : View(producto);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaximoFoto + 65536)]
    public async Task<IActionResult> Editar(int id, Producto producto, IFormFile? fotoProducto)
    {
        if (id != producto.Id) return BadRequest();
        var existente = (await almacen.Productos()).FirstOrDefault(p => p.Id == id);
        if (existente == null) return NotFound();

        producto.Foto = existente.Foto;
        Normalizar(producto);
        if (!ModelState.IsValid) return View(producto);

        var foto = await LeerFoto(fotoProducto);
        if (!ModelState.IsValid) return View(producto);

        if (!await almacen.EditarProducto(producto, foto)) return NotFound();
        TempData["Ok"] = "Producto actualizado.";
        return RedirectToAction(nameof(Administrar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        if (!await almacen.EliminarProducto(id)) return NotFound();
        TempData["Ok"] = "Producto eliminado del catálogo.";
        return RedirectToAction(nameof(Administrar));
    }

    private void Normalizar(Producto producto)
    {
        producto.Nombre = producto.Nombre?.Trim() ?? "";
        producto.Marca = producto.Marca?.Trim() ?? "";
        producto.Marca = MarcaTienda.Todas.FirstOrDefault(m =>
            string.Equals(m.Nombre, producto.Marca, StringComparison.OrdinalIgnoreCase))?.Nombre ?? producto.Marca;

        if (decimal.Round(producto.Precio, 2) != producto.Precio)
            ModelState.AddModelError(nameof(producto.Precio), "Usa como máximo dos decimales.");
    }

    private async Task<FotoProducto?> LeerFoto(IFormFile? archivo)
    {
        if (archivo == null) return null;
        if (archivo.Length is < 12 or > MaximoFoto)
        {
            ModelState.AddModelError("fotoProducto", "Selecciona una imagen válida de hasta 3 MB.");
            return null;
        }

        using var memoria = new MemoryStream((int)archivo.Length);
        await archivo.CopyToAsync(memoria, HttpContext.RequestAborted);
        var datos = memoria.ToArray();
        var extension = DetectarExtension(datos);
        if (extension == null)
        {
            ModelState.AddModelError("fotoProducto", "Usa una imagen JPG, PNG o WebP.");
            return null;
        }

        return new FotoProducto(datos, extension);
    }

    private static string? DetectarExtension(ReadOnlySpan<byte> datos)
    {
        if (datos.StartsWith(new byte[] { 255, 216, 255 })) return ".jpg";
        if (datos.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (datos.StartsWith("RIFF"u8) && datos[8..].StartsWith("WEBP"u8)) return ".webp";
        return null;
    }
}
