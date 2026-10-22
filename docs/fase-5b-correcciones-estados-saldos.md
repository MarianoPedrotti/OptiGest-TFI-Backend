# Fase 5B — Correcciones mínimas de estados y saldos

Fecha: 2026-10-21. Worktree: `backend-estable`, rama
`fix/stable-sales-financing-rules`, HEAD base:
`fd7f7874b99e587f7d3390d06be8973afeb5a6fd`.

## Alcance y reglas

- `PagoVentaService` escribe `PagoParcial` para pagos posteriores incompletos,
  igual que `VentaService` para pagos iniciales. Al completar sigue escribiendo
  `Pagada`. El resumen cuenta `Pendiente`, `PagoParcial` y el alias histórico
  `Parcial`. Las lecturas no normalizan DTO ni actualizan registros históricos.
- El saldo actual es la suma de `Debe - Haber` de TODOS los movimientos del
  cliente: cuenta individual (sobre la colección completa ya cargada), resumen
  por cliente y contador de deudores (suma positiva).
- La lista completa mantiene `Fecha ASC` y agrega `Id ASC` como desempate. La
  lista filtrada mantiene `Fecha DESC` y agrega `Id DESC`. Los filtros de fecha
  solo limitan movimientos, no el saldo actual. La consulta filtrada actual
  devuelve una lista de movimientos, no un saldo actual del período.
- El `Saldo` almacenado en cada movimiento conserva su valor histórico. No se
  recalcula en las lecturas ni se reescribe para coincidir con orden por fecha.
  `Fecha` es la fecha registrada; `Id` identifica la inserción. Mayor Id sigue
  significando último movimiento incorporado, no la fuente del saldo actual.
- Se conserva el cálculo del nuevo movimiento desde la suma previa, la
  transacción y el bloqueo por cliente con `sp_getapplock`. El orden por Id no
  garantiza un orden universal de commit para escritores externos al protocolo.
- Sin cambios en entidades, DTO, interfaces, controllers, esquema, migraciones,
  dependencias ni configuración. Sin publicación ni acceso a producción.

## Consumidores revisados antes de modificar

Backend: VentaService, PagoVentaService, VentaRepository, FinanciacionService,
PagoCuotaService, CuentaCorrienteService, CuentaCorrienteRepository,
OperacionFinancieraRepository, DashboardService y DTO de cuenta corriente.
El filtro defectuoso estaba en el resumen de ventas; los montos diarios y
semanales del dashboard no usan ese filtro.

Frontend estable leído, sin modificaciones: HEAD
`b1487ac0b6c44734e9136fdba0edad6145c4da1f`.

- `ventaElegibilidad.ts` acepta ambos estados parciales.
- `VentaTable.vue` reconoce `PagoParcial`; el alias `Parcial` histórico cae en
  la etiqueta Pendiente cuando no hay financiación.
- `VentasView.vue` filtra por igualdad; PagoParcial no incluye alias Parcial.
- `DashboardUltimasVentas.vue` colorea Parcial, pero PagoParcial cae en el color
  secundario. La escritura canónica hace más visible esta inconsistencia previa.
- El selector `tipoPago=Parcial` de VentaForm describe la modalidad del formulario,
  no una escritura directa del estado de la entidad.
- CuentaCorrienteTable, CuentaCorrienteStats y CuentaCorrienteView consumen
  saldoActual para importes, totales y clasificación de deudores.

Las discrepancias de etiquetas/filtros/colores requieren autorización separada
para frontend antes de corregirlas. No se cambió el contrato para ocultarlas.

## Aislamiento SQL comprobado

Configuración dedicada `tests/OptiGest.Tests/appsettings.Testing.json`:
Environment=Testing, LocalDbInstance=OptiGest_Testing_Phase05,
DatabasePrefix=OptiGest_Testing_Phase05_. No contiene credenciales.

Se inspeccionó TestingDatabase: verifica proceso LocalDB, inventario de nombres,
nombre nuevo con GUID, ausencia previa, marcador de propiedad y destino antes
de crear contextos/escribir. No carga User Secrets ni conexiones de entorno.
Aplica las migraciones existentes solo a una base sintética nueva que le pertenece.

`SqlLocalDB.exe info OptiGest_Testing_Phase05` confirmó instancia detenida al
inicio, versión 15.0.4382.1 (SQL Server 2019 LocalDB). Se inició únicamente esa
instancia y se confirmó pipe `LOCALDB#9B68307C`. No se adoptó SQLEXPRESS ni una
base restaurada. Estos tests no constituyen una ejecución en SQL Server 2022.

Bases creadas y retenidas por el harness:

- Rojo SQL: `OptiGest_Testing_Phase05_4b8a3a110c834b72b792b6d9821755a0`.
- Verde SQL: `OptiGest_Testing_Phase05_9a6fe413a29843f292aa92b7977b703a`.

Se verificó la identidad efectiva en `bin/Debug/net8.0/TestResults/testing-database.json`
del proyecto de tests después de ambas ejecuciones. No se eliminaron bases.
La instancia volvió a `Stopped` al finalizar, mediante stop normal sin force.
No se accedió a Somee, bebikersdb, OptiGest_Restore_Test_20261020, desarrollo,
OptiGest_Central ni tenants.

## Pruebas y resultados

Se agregaron primero 22 casos nuevos: 4 unitarios y 18 SQL. Se amplió además
la prueba concurrente `Two_sales_of_one_client_keep_running_balances_consistent`.

Antes de cambiar aplicación:

- Un error inicial de compilación en los tests nuevos (ClientId por ClienteId)
  fue corregido; no se tomó como reproducción de un defecto de negocio.
- Rojo unitario: 1 aprobado, 3 fallidos por tomar el saldo de la última fila.
- Rojo SQL: 3 aprobados, 15 fallidos por escritura Parcial, resumen incompleto,
  lectura de snapshots históricos y desempate de fechas. Ningún fallo de conexión.
- Las transiciones completas a Pagada ya pasaban.

Después de los cuatro cambios mínimos de aplicación:

| Suite | Aprobados | Fallidos | Omitidos |
| --- | ---: | ---: | ---: |
| Unit normal | 73 | 0 | 0 |
| SQL normal | 86 | 0 | 0 |
| Total de casos distintos | 159 | 0 | 0 |

Compilación Debug correcta de aplicación y tests; permanecen advertencias
preexistentes de nullability. No se generó Release ni se ejecutó E2E de navegador.
Los filtros excluyen Category=PendingRule y las utilidades integradas con opt-in
separado; no se atribuye ejecución a esas utilidades.

Cobertura nueva:

- Pagos incompletos y completos desde Pendiente, PagoParcial y Parcial.
- Lecturas históricas intactas y resumen mixto (3 pendientes y 1 pagada),
  conteo mensual y monto vendido.
- Saldos positivos, cero y negativos en cuenta, resumen y contador.
- Snapshots sintéticos incorrectos conservados; nuevas escrituras calculadas
  desde Debe/Haber sin propagar el snapshot incorrecto.
- Fecha anterior al orden de inserción, fechas iguales y filtros de período.
- Cuenta vacía y coherencia tras dos pagos concurrentes del mismo cliente.

Se conservaron y ejecutaron las pruebas existentes de creación en SaleTests y
SaleAtomicityTests (Pendiente/PagoParcial/Pagada), rollback, financiación,
concurrencia, cierre de última cuota y contratos HTTP.

### Comandos ejecutados

Desde el worktree backend (SQL solo tras verificar/iniciar la instancia dedicada):

```powershell
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --no-restore --filter 'FullyQualifiedName~CurrentAccountTests' --logger 'trx;LogFileName=phase5b-red-unit.trx'
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --no-restore --filter 'Suite=Unit&Category!=PendingRule' --logger 'trx;LogFileName=phase5b-green-unit.trx'
```

Para SQL, se guardó/restauró OPTIGEST_RUN_SQL_TESTS en try/finally; su valor 1
se habilitó exclusivamente dentro del proceso de estos comandos:

```powershell
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ReleaseFinancialRegressionTests' --logger 'trx;LogFileName=phase5b-red-sql.trx'
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --no-build --no-restore --filter 'Suite=Sql&Category!=PendingRule' --logger 'trx;LogFileName=phase5b-green-sql.trx'
```

TRX conservados localmente, ignorados por Git:
`tests/OptiGest.Tests/TestResults/phase5b-{red,green}-{unit,sql}.trx`.

## Límites y revisión pendiente

- SaldoActual presupone Debe/Haber completos, como el cálculo de nuevas escrituras.
  No repara movimientos faltantes ni saldos iniciales implícitos. Si un snapshot
  antiguo es erróneo, saldoActual puede diferir de la fila histórica: es deliberado.
- Consultas separadas pueden observar commits distintos; no se promete una foto
  transaccional común entre endpoints. Cuenta individual suma la misma colección
  que devuelve, sin una segunda consulta independiente.
- El backup de 20/10/2026 contiene solo dos ventas pagadas, no demuestra estos
  escenarios. Todas las regresiones nuevas usan datos sintéticos.
- El artefacto futuro debe identificar el commit local de esta fase; fd7f787 es
  la base y no incluye estas correcciones.
- La autorización de commit local se recibió después de esta validación. La
  preparación de Release sigue pendiente de autorización separada.
