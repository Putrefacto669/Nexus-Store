using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentasMVC.Models;

namespace VentasMVC.Controllers;

public sealed class AdministracionController(IConfiguration configuracion) : Controller
{
    [HttpGet]
    public IActionResult Entrar(string? returnUrl)
    {
        if (User.IsInRole("Administrador")) return Volver(returnUrl);
        return View(new AccesoAdministrador { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(AccesoAdministrador modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = configuracion["Administrador:Usuario"];
        var contrasena = configuracion["Administrador:Contrasena"];
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            ModelState.AddModelError("", "Configura el usuario y la contraseña del administrador según el README.");
            return View(modelo);
        }

        var contrasenaCorrecta = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(modelo.Contrasena)),
            SHA256.HashData(Encoding.UTF8.GetBytes(contrasena)));
        if (!contrasenaCorrecta || !string.Equals(usuario, modelo.Usuario, StringComparison.Ordinal))
        {
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        var identidad = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, usuario),
            new Claim(ClaimTypes.Role, "Administrador")
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identidad));
        return Volver(modelo.ReturnUrl);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult Volver(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Administrar", "Productos");
}
