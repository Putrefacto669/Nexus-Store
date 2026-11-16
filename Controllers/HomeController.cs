using Microsoft.AspNetCore.Mvc;
using VentasMVC.Services;
namespace VentasMVC.Controllers;
public class HomeController(AlmacenPocketBase almacen, Monedas monedas) : Controller
{
    public async Task<IActionResult> Index()
    {
        var productos = await almacen.Productos();
        var ventas = await almacen.Ventas();
        ViewBag.Productos = productos.Count;
        ViewBag.Stock = productos.Where(p => p.EnVenta).Sum(p => (long)p.Stock);
        ViewBag.Ventas = ventas.Count;
        ViewBag.Ingresos = ventas.Sum(v => v.Total);
        ViewBag.IngresosUsd = monedas.FormatearDolares(ventas.Sum(v => monedas.ConvertirADolares(v.Total, v.CordobasPorDolar)));
        ViewBag.Ultimas = ventas.OrderByDescending(v => v.Id).Take(5).ToList();
        return View();
    }
    public IActionResult Error() => View();
}
