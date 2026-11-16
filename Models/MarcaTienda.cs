namespace VentasMVC.Models;

public sealed record ArteMarca(string Archivo, string Titulo, string Alternativo, string Credito, string Fuente);

public sealed record MarcaTienda(string Id, string Nombre, string Descripcion, string? Logo,
    ArteMarca Portada, ArteMarca Destacado)
{
    public static IReadOnlyList<MarcaTienda> Todas { get; } = Array.AsReadOnly(new MarcaTienda[]
    {
        new("nintendo", "Nintendo", "Consolas y juegos para disfrutar en cualquier lugar.", null,
            new("mario-wonder", "Super Mario Bros. Wonder", "Mario y Luigi en el Reino Flor", "Nintendo",
                "https://www.nintendo.com/us/store/products/super-mario-bros-wonder-switch/"),
            new("zelda-tears", "The Legend of Zelda: Tears of the Kingdom", "Zelda y Link en Hyrule", "Nintendo",
                "https://www.nintendo.com/us/store/characters/tears-of-the-kingdom/")),
        new("playstation", "PlayStation", "Consolas, juegos y accesorios para tu PlayStation.", "Playstation/PS LOGO.jpg",
            new("kratos-ragnarok", "God of War Ragnarök", "Kratos y Atreus en un paisaje helado", "PlayStation",
                "https://www.playstation.com/en-us/games/god-of-war-ragnarok/"),
            new("crash-bandicoot", "Crash Bandicoot 4: It's About Time", "Crash y Coco saltan sobre un portal", "Activision",
                "https://www.activision.com/games/crash/crash-bandicoot-4")),
        new("xbox", "Xbox", "Encuentra juegos, consolas y accesorios para Xbox.", "Xbox/Xbox LOGO.jpg",
            new("halo-infinite", "Halo Infinite", "Jefe Maestro ante las montañas de Zeta Halo", "Xbox Wire",
                "https://news.xbox.com/en-us/2021/12/08/halo-infinite-available-now-with-xbox-game-pass/"),
            new("forza-horizon", "Forza Horizon 5", "Un auto deportivo rojo en un camino rural", "Xbox",
                "https://www.xbox.com/en-US/games/forza-horizon-5")),
        new("nvidia", "NVIDIA", "Tarjetas gráficas, hardware y software para tu PC.", "NVIDIA/nvidia-logo-dark-2880x1800-9996.jpg",
            new("geforce-rtx", "GeForce RTX 5090", "Tarjeta GeForce RTX 5090 sobre una trama verde", "NVIDIA",
                "https://nvidianews.nvidia.com/news/nvidia-blackwell-geforce-rtx-50-series-opens-new-world-of-ai-computer-graphics"),
            new("portal-rtx", "Portal con RTX", "Un portal azul y un cubo en una cámara de pruebas", "NVIDIA",
                "https://www.nvidia.com/es-la/geforce/news/portal-with-rtx-ray-tracing/"))
    });
}

public sealed record CatalogoViewModel(IReadOnlyList<Producto> Productos, MarcaTienda? Marca = null);
