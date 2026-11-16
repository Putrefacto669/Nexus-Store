# NEXUS STORE — ASP.NET Core MVC (.NET 8) y PocketBase

Sistema de ventas con catálogo por marca, inventario, fotos y registro de ventas en PocketBase.

## Ejecutar en Windows

1. Instala el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) y extrae [PocketBase](https://pocketbase.io/docs/) en una carpeta local. La integración se verificó con PocketBase 0.42.2.
2. Para una instalación nueva, crea una clave para la comunicación entre VentasMVC y PocketBase. Ejecuta desde la carpeta del proyecto:

```powershell
$clavePocketBase = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "PocketBase:Clave" $clavePocketBase
```

3. En una terminal, inicia PocketBase con las migraciones y las rutas de Nexus:

```powershell
.\PocketBase\Iniciar-PocketBase.ps1 -Ejecutable "C:\ruta\a\pocketbase.exe"
```

   Utiliza `-Datos "C:\ruta\a\pb_data"` si la base de datos está en otra ubicación. Si PocketBase ya está sirviendo en el puerto 8090, detén esa instancia antes de iniciarla con el script. El panel de PocketBase queda en [127.0.0.1:8090/_/](http://127.0.0.1:8090/_/) y utiliza su propia cuenta de superusuario.

4. En otra terminal, ejecuta `dotnet run` desde VentasMVC y abre [localhost:5000](http://localhost:5000).

Si se configura otro servidor, usa `PocketBase:Url` en secretos de desarrollo o `PocketBase__Url` en el entorno de VentasMVC. Para ejecutar fuera del perfil de desarrollo, define `PocketBase__Clave` en el entorno de ambos servicios. La clave permanece en los servidores.

## Datos y migración

En el primer arranque con una base nueva, VentasMVC importa `Data/productos.json`, `Data/ventas.json` y las fotos referenciadas en `Data/fotos/`. La importación conserva la numeración, el stock registrado y el historial. Puedes guardar los archivos originales como respaldo. Los siguientes arranques leen directamente de PocketBase.

- `nexus_productos` contiene el catálogo, el stock, los precios en centavos de córdoba y las fotos.
- `nexus_ventas` guarda el historial con sus precios unitarios y la tasa de cambio de cada venta.
- `nexus_estado` guarda la numeración y el estado de la importación.

PocketBase controla la validación del stock y confirma la venta y el descuento de existencias en una misma transacción. Las migraciones están en `PocketBase/pb_migrations/` y las rutas internas en `PocketBase/pb_hooks/`.

## Administrador de Nexus

Para preparar otra copia del proyecto o cambiar las credenciales de Nexus, configura desde PowerShell:

```powershell
dotnet user-secrets set "Administrador:Usuario" "admin"
dotnet user-secrets set "Administrador:Contrasena" "elige-una-contraseña-propia"
```

Abre **Administrador → Registrar producto**. El panel permite editar precios y existencias, reemplazar fotos y retirar productos de la venta. En otros entornos se pueden configurar las credenciales con `Administrador__Usuario` y `Administrador__Contrasena`.

## Monedas

Introduce el precio en córdobas. La tienda muestra el equivalente en USD con una tasa interna configurable. El valor inicial de ejemplo es C$ 36.62 por dólar. Para cambiarlo, configura `Monedas__CordobasPorDolar` o, en desarrollo, ejecuta `dotnet user-secrets set "Monedas:CordobasPorDolar" "36.62"` con el valor que corresponda. Las ventas nuevas conservan la tasa aplicada. Las ventas anteriores sin tasa guardada se muestran con la tasa configurada.

## Funcionalidades

- Dashboard de productos, existencias, ventas e ingresos.
- Catálogos de Nintendo, PlayStation, Xbox y NVIDIA con búsqueda por nombre, marca o categoría.
- Registro, edición y eliminación de productos desde el panel de administrador.
- Carrito, historial y comprobante imprimible.

El proyecto está preparado para uso local. El carrito registra operaciones de inventario y ventas.
