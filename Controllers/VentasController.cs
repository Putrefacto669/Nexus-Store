using Microsoft.AspNetCore.Mvc;
using VentasMVC.Models;
using VentasMVC.Services;
namespace VentasMVC.Controllers;
public class VentasController(AlmacenPocketBase almacen) : Controller
{
    public async Task<IActionResult> Index() => View((await almacen.Ventas()).OrderByDescending(v => v.Id).ToList());
    public async Task<IActionResult> Crear()
    {
        ViewBag.Productos = (await almacen.Productos()).Where(p => p.EnVenta).OrderBy(p => p.Nombre).ToList();
        return View(new NuevaVentaViewModel());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(NuevaVentaViewModel modelo)
    {
        if (modelo.Cliente?.Length > 120) ModelState.AddModelError(nameof(modelo.Cliente), "Máximo 120 caracteres.");
        if (!ModelState.IsValid)
        {
            ViewBag.Productos = (await almacen.Productos()).Where(p => p.EnVenta).ToList();
            return View(modelo);
        }
        var (venta, error) = await almacen.RegistrarVenta(modelo);
        if (error != null)
        {
            ModelState.AddModelError("", error);
            ViewBag.Productos = (await almacen.Productos()).Where(p => p.EnVenta).ToList();
            return View(modelo);
        }
        TempData["Ok"] = $"Venta #{venta!.Id} registrada correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = venta.Id });
    }
    public async Task<IActionResult> Detalle(int id)
    {
        var venta = (await almacen.Ventas()).FirstOrDefault(v => v.Id == id);
        return venta == null ? NotFound() : View(venta);
    }
}
