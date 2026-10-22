# Fase 4 - Orden de ventas y financiaciones

## Alcance

- `VentaRepository.ObtenerTodosAsync` ordena por `Fecha` descendente y, ante
  empate, por `Id` descendente antes de materializar el listado.
- `FinanciacionRepository.ObtenerTodosAsync` ordena por `FechaCreacion`
  descendente y, ante empate, por `Id` descendente antes de materializar el
  listado.
- `FechaCreacion` representa el alta de financiacion: `FinanciacionService`
  la asigna con `DateTime.Now` antes de persistirla.
- `VentaService.ObtenerTodosAsync` informa `FinanciacionId` cuando la venta
  tiene financiacion, usando la navegacion ya incluida por el repository.

## Contrato y persistencia

- No se cambiaron DTOs publicos, entidades, migraciones, relaciones ni
  formulas financieras.
- El orden se resuelve en el backend, fuente de los listados generales. No se
  ordenan ventas ni financiaciones nuevamente en el frontend.

## Pruebas

- Se activaron las dos reglas SQL antes marcadas `PendingRule`: orden de
  ventas y orden de financiaciones con fechas iguales.
- Se agrego una prueba SQL que confirma el `FinanciacionId` del listado de
  ventas financiadas.
- La ejecucion usa la instancia LocalDB exclusiva `OptiGest_Testing_Phase05`
  y datos sinteticos del harness existente.
