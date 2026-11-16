# Publicar Nexus Store en Render

Nexus Store necesita dos servicios: ASP.NET Core MVC y PocketBase.

## PocketBase (primero)

1. En Render crea un **Private Service**, elige el repositorio Nexus-Store, rama `main`, Dockerfile `./Dockerfile.pocketbase`.
2. Selecciona un plan que permita disco persistente; monta ese disco en `/pb/pb_data`. No uses almacenamiento efímero para ventas reales.
3. Define la variable secreta `PocketBase__Clave` con una clave aleatoria de al menos 32 bytes, generada y almacenada fuera de Git.
4. Despliega, y toma nota de la dirección interna del servicio. Los hooks y migraciones están incluidos.

## Tienda ASP.NET Core MVC

1. Crea un **Web Service** desde el mismo repositorio, rama `main`, Dockerfile `./Dockerfile`.
2. Configura en Environment:

| Variable | Valor |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `PocketBase__Url` | URL interna de PocketBase, p. ej. `http://HOST_INTERNO:10000` |
| `PocketBase__Clave` | La misma clave de PocketBase |
| `Administrador__Usuario` | Usuario para el panel |
| `Administrador__Contrasena` | Contraseña robusta y única |
| `Monedas__CordobasPorDolar` | Tasa configurada, p. ej. `36.62` |

3. Despliega. Render entregará una dirección pública `*.onrender.com`.

## Advertencias

- Render usa la variable `PORT` (10000 por defecto). Los dos Dockerfiles escuchan en `0.0.0.0` usando esa variable.
- El proyecto importa `Data/productos.json` y `Data/ventas.json` solo si están presentes. `Data/` está ignorada por Git; no se migrarán automáticamente los datos que tengas solo en tu PC.
- `Program.cs` conecta con PocketBase antes de comenzar a servir; despliega PocketBase primero.
- Prueba fotos, productos, login, stock, ventas e historial, y crea respaldos del disco persistente.
- Mantén PocketBase como servicio privado para no exponer el panel a internet.

Documentación: https://render.com/docs/docker · https://render.com/docs/disks · https://pocketbase.io/docs/
