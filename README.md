# NEXUS STORE — ASP.NET Core MVC (.NET 8)

Sistema de ventas didáctico inspirado visualmente en el cliente de Steam. **No utiliza base de datos**: guarda inventario y ventas en archivos JSON creados en `Data/` cuando se inicia el programa.

## Ejecutar

1. Instalar [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Abrir `VentasMVC.csproj` en Visual Studio 2022 (o superior) y ejecutar, o usar terminal:

```powershell
dotnet run
```

3. Abrir http://localhost:5000. El perfil local de arranque carga los secretos de desarrollo automáticamente.

## Administrador local

En la carpeta del `.csproj`, ejecuta `dotnet user-secrets list` para consultar las credenciales locales. Si estás preparando otra copia del proyecto o quieres cambiar la contraseña, configura tus credenciales desde PowerShell.

```powershell
dotnet user-secrets set "Administrador:Usuario" "admin"
dotnet user-secrets set "Administrador:Contrasena" "elige-una-contraseña-propia"
dotnet run
```

Abre **Administrador → Registrar producto**. El panel también permite editar existencias, reemplazar fotos y retirar productos de la venta. Las fotos se guardan en `Data/fotos/` junto al catálogo local. En otros entornos se pueden configurar las credenciales con las variables `Administrador__Usuario` y `Administrador__Contrasena`.

## Monedas

Introduce el precio en córdobas. La tienda muestra el equivalente en USD con una tasa interna configurable. El valor inicial de ejemplo es C$ 36.62 por dólar. Para cambiarlo, configura `Monedas__CordobasPorDolar` o, en desarrollo, ejecuta `dotnet user-secrets set "Monedas:CordobasPorDolar" "36.62"` con el valor que corresponda. Las ventas nuevas conservan la tasa aplicada. Las ventas anteriores se muestran con la tasa configurada.

## Funcionalidades

- Dashboard con indicadores de productos, existencias, ventas e ingresos.
- Catálogo con secciones para Nintendo, PlayStation, Xbox y NVIDIA, y búsqueda por nombre, marca o categoría.
- Registro, edición y eliminación de productos mediante el panel de administrador.
- Carrito de compras, validación de stock del lado del servidor y registro de ventas.
- Historial y comprobante imprimible.
- Archivos JSON persistentes, inicializados con 3 productos de ejemplo.

## Estructura

- `Controllers/`: lógica MVC.
- `Models/`: productos, ventas y detalle.
- `Views/`: interfaz Razor rediseñada.
- `wwwroot/css/site.css`: tema visual NEXUS.
- `Services/AlmacenJson.cs`: almacenamiento local sincronizado por un semáforo.

## Limitaciones

Es un prototipo para una única instancia local, con una cuenta de administración configurable, sin pago real, impuestos, facturación electrónica ni transacciones ACID entre los dos archivos JSON. No está pensado para múltiples servidores o producción.

El diseño general parte del tema Nexus inspirado en Steam. Las secciones de marca usan los logos disponibles en `Assets/`.
