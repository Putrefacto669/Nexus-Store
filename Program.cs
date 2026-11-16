using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using VentasMVC.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<AlmacenPocketBase>((servicios, cliente) =>
{
    var configuracion = servicios.GetRequiredService<IConfiguration>();
    var url = configuracion["PocketBase:Url"] ?? "http://127.0.0.1:8090";
    var clave = configuracion["PocketBase:Clave"];
    if (string.IsNullOrWhiteSpace(clave))
        throw new InvalidOperationException("Configura PocketBase:Clave según el README.");
    cliente.BaseAddress = new Uri(url.TrimEnd('/') + "/api/nexus/");
    cliente.DefaultRequestHeaders.Add("X-Nexus-Key", clave);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<Monedas>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Administracion/Entrar";
        opciones.AccessDeniedPath = opciones.LoginPath;
        opciones.Cookie.Name = "NexusStore.Administracion";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("es-NI"));
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Assets")),
    RequestPath = "/marcas"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
await app.Services.GetRequiredService<AlmacenPocketBase>().ImportarDatosAnteriores();
app.Run();
