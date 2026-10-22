# SistemaDeVentas Backend
API ASP.NET Core para la logica de ventas, clientes, productos, vehiculos, financiacion, pagos, usuarios y dashboard del sistema.

## Stack
- Lenguaje: C# con nullable habilitado e implicit usings.
- Framework / runtime: ASP.NET Core 8 Web API.
- Base de datos: SQL Server con Entity Framework Core 8.
- Tests: no hay proyecto de tests detectado en esta solucion.

## Comandos
- `dotnet run --project SistemaDeVentas.csproj --launch-profile https` — arranca la API desde esta carpeta con Swagger en Development.
- `dotnet restore SistemaDeVentas.sln` — restaura paquetes NuGet.
- `dotnet build SistemaDeVentas.sln` — compila la API.
- `dotnet ef migrations add <Nombre>` — crea una migracion si `dotnet-ef` esta instalado y cambiaste entidades o `AppDbContext`.

## Estructura del proyecto
- `Program.cs` — wiring real: DI, CORS, JWT, Swagger, autorizacion global y fallback SPA.
- `Controllers/` — endpoints `api/[controller]`; controllers traducen HTTP y delegan en servicios.
- `Services/` y `Services/Interfaces/` — validaciones de negocio, mapeo DTO y orquestacion.
- `Repositories/` y `Repositories/Interfaces/` — acceso EF Core y `SaveChangesAsync`.
- `Entities/`, `DTOs/`, `Data/AppDbContext.cs`, `Migrations/` — modelo persistido, contratos de API, configuracion EF y migraciones.

## Convenciones
- Mantene el flujo `Controller -> Service -> Repository`; no saltees capas para resolver rapido.
- Los controllers no deben exponer entidades EF directamente; usa DTOs por modulo en `DTOs/<Modulo>/`.
- Al agregar un modulo, crea interfaces e implementaciones de repository/service y registra ambos pares en `Program.cs`.
- Para datos del usuario autenticado, usa claims; `ClienteController.Crear` lee `ClaimTypes.NameIdentifier` para asignar `UsuarioId`.
- Los roles validos estan en `Helpers/Roles.cs`: `Admin`, `Dueno`, `Vendedor`.
- Configura indices unicos, relaciones y precision decimal en `Data/AppDbContext.cs` antes de crear migraciones.

## No hagas
- No agregues `DefaultConnection` ni `Jwt:Key` reales a `appsettings*.json`.
- No trates `SistemaDeVentas.http` como contrato vigente: todavia referencia `weatherforecast`, excluido del `.csproj`.
- No cambies roles con strings sueltos si existe `Helpers/Roles.cs`.
- No uses carpetas `publish*` o `deploy-somee/` para inferir codigo actual.

## Flujo de trabajo
- Antes de una tarea no trivial, propone un plan y espera mi OK.
- Una tarea a la vez; al terminar, dime que cambiaste para que lo revise.
- Si no estas seguro al 80%, pregunta. No inventes.

## Documentacion
- Reglas generales del repo: `../AGENTS.md`.
- Archivo con nombre raro: `Controllers/AuthController .cs` tiene un espacio antes de `.cs`; usa la ruta exacta al editarlo.
