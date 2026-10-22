# Fase 5A — Entorno integrado y regresión

Fecha de cierre: 2026-10-20. La validación original registró hallazgos frontend,
resueltos y revalidados durante el cierre técnico de Fase 5A.1. No es una aprobación
de publicación.

## Versiones verificadas

- Backend: `fix/stable-sales-financing-rules`, `f8ba7d4fe7f43f1e6ed09d852e5f324b9ceebaeb`.
- Frontend: `fix/stable-sales-financing-ui`, `13f5cffca55b41ecaf5eba5d34bc204902a5e0d6`.
- Ambos worktrees estaban limpios al comenzar. No se modificó código funcional,
  contratos, entidades, migraciones, dependencias ni los directorios originales.

## Aislamiento ejecutado

La instancia exclusiva fue `OptiGest_Testing_Phase05`. El harness existente
`TestingDatabase` verificó nombre, proceso LocalDB, inventario de bases permitidas,
ausencia previa del nombre generado y marcador de propiedad antes de migrar/sembrar.

Ejecución integrada utilizada:

- Directorio: `.testing-data/phase5a-e019a8e4159d48479d100b59ef066f27/`.
- Base: `OptiGest_Testing_Phase05_64938c187c654bfe91b614cc97cf6142`.
- Evidencia no sensible: `isolation.json` dentro de ese directorio.
- Migraciones existentes aplicadas exclusivamente a la base nueva:
  `20260722170709_InitialCreate` y `20260826180324_ConfigurarPrecisionDecimales`.
- Migraciones pendientes en esta base: ninguna.
- No se reutilizaron bases de ejecuciones anteriores ni se borraron bases.

El launcher ejecutó el ensamblado real de la API, incluyendo su `Program.cs`,
controllers, servicios, repositorios, password hasher y validación JWT. La extensión
de startup de tests agrega una comprobación de conexión efectiva y propiedad al
inicio y antes de cada `SaveChangesAsync`; rechaza escrituras síncronas.

El proceso recibió una lista permitida de variables de sistema y herramientas.
`DOTNET_ENVIRONMENT` y `ASPNETCORE_ENVIRONMENT` fueron `Testing`; el content root
contenía solo configuración generada para esta ejecución. No se cargó el proveedor
de User Secrets, perfiles de lanzamiento ni configuración de aplicaciones heredada.
Se verificaron destino LocalDB, base, Integrated Security, MARS=false, Pooling=false
y marcador. La clave JWT y las contraseñas se generaron para testing.

La configuración y `session.local.json` quedan ignorados en `.testing-data/`;
no copiar sus credenciales a documentación o a Git.

## Cierre técnico Fase 5A.1

`Start-IntegratedTesting.ps1` exige que la instancia dedicada esté detenida antes
de iniciar una ejecución. Una vez que el launcher inicia LocalDB, cualquier error
posterior de preparación o arranque limpia solamente los procesos cuya identidad,
línea de comandos y fecha de creación corresponden a esa ejecución, y detiene la
instancia exclusiva. No borra bases, conserva transcript y logs de API/Vite, y
vuelve a propagar el error original.

La corrección se verificó con un fallo controlado de Vite después de preparar una
base nueva y antes de crear `processes.json`: la API propia se detuvo, LocalDB
volvió a `Stopped`, los puertos quedaron libres y los logs de la ejecución
permanecieron en `.testing-data/`. También se confirmó un arranque normal seguido
del cierre existente: ambos procesos propios y LocalDB se detuvieron, sin borrar
la base sintética.

API: `127.0.0.1:51850`; frontend Vite: `127.0.0.1:51851`. Se comprobaron puertos
libres antes del arranque y listeners únicamente sobre loopback. Vite usó un archivo
de configuración exclusivo, sin cargar `.env`, con API local fija y puerto estricto.
El contexto Playwright bloqueó service workers y solicitudes HTTP/WebSocket externas.
Una sonda a `qa-egress-probe.example.invalid` fue abortada antes de salir a la red.

## Datos y persistencia comprobados

Usuarios sintéticos Admin y Vendedor, cinco clientes, dos productos, seis vehículos
y cuatro planes. Login real desde navegador para ambos roles.

La verificación final abrió un contexto SQL nuevo y de solo lectura, usando únicamente
la configuración de esta ejecución y verificando de nuevo el marcador. Pasaron
18 aserciones. Evidencia:

` .testing-data/phase5a-e019a8e4159d48479d100b59ef066f27/database-verification-20261020033205.json `

| Dato | Persistencia final |
| --- | --- |
| Ventas | 6, total $4.650 |
| Artículos consolidados de venta 1 | 5 cascos + 2 guantes, total $600 |
| Stock final | 5 cascos, 17 guantes |
| Vehículos vendidos | 4 |
| Financiaciones | 2: una cancelada, otra activa |
| Cuotas | 5; tres pagadas |
| Pagos de cuotas | 3; $293,33 + $293,33 + $293,34 = $880 |
| Cuenta cliente 1 | Venta $1.000 − entrega $200 + interés $80 − cuotas $880 = $0 |
| Saldo contable global de los clientes sintéticos | $2.600 |

Se rechazaron venta mixta (400), stock insuficiente (409), doble financiación (409),
pago directo de venta financiada (409), financiación de venta pagada (409) y pago
duplicado de cuota (409). Las comprobaciones SQL confirmaron ausencia de duplicados.

## Resultados de regresión

- Build completo: `dotnet build SistemaDeVentas.sln --no-incremental --no-restore`:
  correcto, 0 errores, 73 advertencias de nullability preexistentes.
- Suite normal final: **137 aprobadas, 0 fallidas**, incluyendo 69 unitarias y
  68 SQL (integridad, atomicidad, concurrencia, ordenamiento y cinco casos HTTP).
- Dos utilidades nuevas E2E se omiten explícitamente sin sus opt-ins. No se omitió
  ninguna de las 137 pruebas de negocio. Preparación y verificación se ejecutaron
  por separado; la verificación final aprobó 1 prueba con 18 aserciones SQL.
- TRX final conservado:
  `.testing-data/results/phase5a-regression-20f5a51faff34f0fab7d3b3013a8b884.trx`.
- Frontend: build y `test:typecheck` correctos; **39 Vitest aprobadas, 0 fallidas**.

Incidentes del harness, corregidos sin tocar la aplicación:

1. Primer arranque rechazado por ausencia del proveedor SQL en el registro de contexto
   del hook. Su base `OptiGest_Testing_Phase05_e48d9a4efdf546ccb04992cf6d8e0318` y logs
   en `.testing-data/phase5a-99b207ed202e4f53b85681b28b85bc25/` se conservaron. El flujo
   integrado se realizó después en una base nueva.
2. El atributo HostingStartup fue descubierto también por TestServer, causando cinco
   fallos HTTP en la primera regresión (132 aprobadas). Se limitó la activación al
   launcher mediante `OPTIGEST_E2E_HOST=1`; la repetición aprobó las 137 pruebas.
   El reporte fallido sigue en `.testing-data/results/characterization.trx`.
3. Al cierre Windows había reutilizado el PID de la API para otro proceso. El script
   comprobó identidad/fecha, no lo terminó y verificó los puertos libres.

## Herramientas añadidas y reproducción

- `scripts/Start-IntegratedTesting.ps1`: sanea entorno, prepara una base nueva,
  genera configuración y arranca los dos procesos.
- `scripts/Stop-IntegratedTesting.ps1`: comprueba identidad de procesos, detiene solo
  los propios, verifica puertos y opcionalmente detiene LocalDB exclusiva; no elimina bases.
- `tests/OptiGest.Tests/Integrated/IntegratedEnvironment.cs`: preparación y lectura
  de persistencia del escenario documentado, ambas con opt-in explícito.
- `tests/OptiGest.Tests/Integrated/TestingStartup.cs`: protección de escrituras de la API E2E.

```powershell
powershell -NoProfile -File scripts/Start-IntegratedTesting.ps1 -FrontendRoot C:\Users\Pc\Desktop\OptiGest-Worktrees\frontend-estable\frontend

# Tras ejecutar los escenarios del navegador en la NUEVA base:
$env:OPTIGEST_E2E_ROOT = '<directorio de esta ejecución>'
$env:OPTIGEST_VERIFY_E2E = '1'
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --filter 'FullyQualifiedName~Verify_current_run_persistence_read_only'

powershell -NoProfile -File scripts/Stop-IntegratedTesting.ps1 -RunDirectory '<directorio de esta ejecución>' -StopDedicatedSql
```

Las aserciones de persistencia corresponden a los seis escenarios y cantidades de
esta ejecución; no son un verificador genérico de bases arbitrarias.

## Cierre

API y frontend detenidos con sus procesos hijos. Puertos 51850/51851 sin listeners.
Contextos de navegador de testing cerrados. LocalDB exclusiva detenida; todas las
bases y evidencias retenidas. Hay un `shutdown-*.json` dentro del directorio de ejecución.

Cambios de testing y documentación revisados y registrados en commits locales de
cierre. No hubo push, merge ni despliegue.
Informe funcional: `../../frontend-estable/docs/fase-5a-validacion-integral.md`.
Investigación de producción: `fase-5a-investigacion-despliegue.md`.
