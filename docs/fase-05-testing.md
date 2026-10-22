# Fase 0.5 - Infraestructura de testing backend

## Entornos permanentes

- Backend: `C:/Users/Pc/Desktop/OptiGest-Worktrees/backend-estable`, rama
  `fix/stable-sales-financing-rules`, HEAD/base `0450b9bf898863eb5308d240136ee8e7f4fe531c`.
- Frontend: `C:/Users/Pc/Desktop/OptiGest-Worktrees/frontend-estable`, rama
  `fix/stable-sales-financing-ui`, HEAD/base `513ce535c3b056e6f70cf8cea3cf5655b0a62950`.

Se comprobaron destinos inexistentes y se utilizo git worktree move. Los
documentos de Fase 0 se conservaron byte a byte (hash backend
32c03e29908f7c816574faa2c185f2b5b33d98f9; frontend
130afcffdcb2070473726aafbf4b6ae5e9168f52). Sus rutas Temp son historicas;
este informe establece las rutas actuales. El README del directorio padre
explica que ambos son worktrees vinculados, no clones de produccion.

## Cambios de alcance exclusivo testing

- Proyecto separado `tests/OptiGest.Tests/OptiGest.Tests.csproj`, target net8.0,
  con carpetas Unit y Sql.
- Dependencias fijadas: Microsoft.NET.Test.Sdk 17.12.0, xunit 2.9.3,
  xunit.runner.visualstudio 2.8.2 y Moq 4.20.72; packages.lock.json de tests.
- La solucion agrega el proyecto; el csproj Web excluye tests de Compile,
  Content, None y EmbeddedResource para no compilar/copiar infraestructura de
  pruebas al producto. No se modificaron paquetes del proyecto Web.
- `.gitignore` excluye resultados y datos locales.
- `scripts/Test-Testing.ps1` inicia solo la instancia ya provisionada y activa
  la ejecucion SQL dentro de su proceso; restaura la variable al terminar.
- No se modificaron Program.cs, Services, Repositories, DTOs, Entities, Data ni
  archivos de migraciones. No hay commits ni push.

## LocalDB exclusiva y comprobaciones de destino

Se creo una instancia NUEVA: `OptiGest_Testing_Phase05`, LocalDB SQL Server 2019
15.0.4382.1, privada del usuario local. Antes solo existia MSSQLLocalDB.
Se comprobo su nombre/propietario/pipe con SqlLocalDB info y se conecto a su
master exclusivamente para comprobar IsLocalDB=1 e inventario: inicialmente
solo master, tempdb, model y msdb. No se uso SQLEXPRESS ni MSSQLLocalDB.

Cada ejecucion SQL crea otra base con nombre generado:
`OptiGest_Testing_Phase05_<GUID de 32 caracteres>`. Todas se retienen para
inspeccion; no hay EnsureDeleted, DROP DATABASE ni limpieza automatica.
La ultima ejecucion de requisitos pendientes uso:
`OptiGest_Testing_Phase05_37f356b465bc467d84ba19d93c3d8459`.
Los MDF/LDF se crean en el directorio de datos predeterminado de esta LocalDB
(MDF observado bajo `C:/Users/Pc/`, con el mismo nombre unico). No se sobrescribe
ningun archivo existente ni se reutilizan bases de otras ejecuciones.

TestingDatabase aplica estas barreras antes de escribir:

1. Opt-in `OPTIGEST_RUN_SQL_TESTS=1`.
2. JSON exclusivo de tests con entorno Testing, instancia y prefijo exactos;
   sin ConfigurationBuilder predeterminado, User Secrets ni conexiones de entorno.
3. SqlLocalDB info verifica existencia e instancia arrancada; el proceso efectivo
   SQL debe coincidir con el pipe de esa instancia y reportar IsLocalDB=1.
4. Rechazar la instancia si aparecen bases de usuario con nombres ajenos al patron.
5. Nombre de base aleatorio validado y DB_ID inexistente antes de CREATE DATABASE.
6. Marcador propio dbo.OptiGestTestingIdentity con token por ejecucion y DB_NAME.
7. Verificacion del marcador/servidor antes de migrar y antes de cada SaveChanges
   asincrono; se rechazan escrituras sincronas desde el contexto de tests.

Se aplicaron las DOS migraciones EXISTENTES, sin modificarlas, solo en cada
base nueva y verificada. Datos totalmente sinteticos (emails example.invalid,
usuarios sin password real, productos/vehiculos/clientes de tests).

Configuracion Testing: MARS=false, sin EnableRetryOnFailure, Integrated Security,
Pooling=false y conexion creada por el harness. No se inicia ASP.NET ni se leen
User Secrets: los tests de integracion llaman servicios/repositorios reales.
No se necesita JWT ni puerto en este alcance. Las pruebas HTTP autenticadas y
E2E con API levantada quedan para ampliacion posterior con JWT sintetico.

## Ejecucion reproducible

Desde la raiz del worktree backend:

```powershell
dotnet build SistemaDeVentas.sln
dotnet test tests/OptiGest.Tests/OptiGest.Tests.csproj --filter "Suite=Unit&Category!=PendingRule"
powershell -NoProfile -File scripts/Test-Testing.ps1
powershell -NoProfile -File scripts/Test-Testing.ps1 -PendingRules
```

El script requiere SqlLocalDB.exe en PATH y la instancia exclusiva existente.
Si no existe, se detiene; no crea/adopta silenciosamente otra instancia.
LocalDB puede detenerse por inactividad: el script la arranca explicitamente.
El ultimo comando retorna 1 mientras las reglas esten sin implementar.
`dotnet test` sin filtros ejecuta tambien requisitos pendientes y NO debe
interpretarse como suite verde. Sin opt-in, los SqlFact se omiten con motivo.

La identificacion de la ultima base se escribe en el archivo ignorado:
`tests/OptiGest.Tests/bin/Debug/net8.0/TestResults/testing-database.json`.
Resultados TRX ignorados: `.testing-data/results/characterization.trx` y
`.testing-data/results/pending.trx`.

## Resultados finales

- Build completo sin incremental: correcto, 0 errores, 73 advertencias
  CS8618/CS8602 preexistentes en codigo de aplicacion.
- Caracterizacion/infraestructura: 31 exitosas (24 unitarias y 7 SQL), 0 fallidas,
  0 omitidas con opt-in.
- Requisitos pendientes: 12 fallidas por aserciones de comportamiento actual
  (7 unitarias y 5 SQL), no por conexion/configuracion.

Las pruebas SQL verifican desde un contexto NUEVO, sin transaccion exterior:

- Exito de financiacion: N cuotas y suma exacta del total.
- Control del harness: una transaccion SQL explicita realmente revierte cambios.
- Fallo antes de cuota 2: el sistema actual deja 1 financiacion, 1 cuota e
  interes 100. La prueba de caracterizacion lo documenta; la de aceptacion exige 0.
- Fallo en credito de cuenta de una venta: quedan venta, pago inicial 50,
  stock descontado a 8 y debito 200. Aceptacion exige revertir todo.
- El seed incluye una venta historica sintetica con su debito 1000 y vehiculo
  vendido. Las aserciones de rollback comparan DELTAS respecto de ese seed;
  no exigen borrar datos previos ni reescribir saldos historicos.
- Dos pagos de 800 sobre venta 1000: dos contextos/conexiones y barrera de tests
  permiten reproducir 1600 pagados. Aceptacion exige no superar el total.
- Listados de ventas y financiaciones: fallan las aserciones de Id descendente
  con fechas iguales. El orden debe garantizarse en backend; no se exige duplicar
  el sort en la UI.

Pendientes unitarios: no financiar articulos, rango por saldo, no venta mixta,
no segundo vehiculo (distinto o repetido), consolidacion y stock acumulado.

## Interpretacion y limitaciones

Las caracterizaciones de defectos NO son criterios de aceptacion comercial.
Al implementar cada correccion, retirar/actualizar su caracterizacion y activar
el requisito equivalente en la suite normal; no conservar tests que exijan bugs.

Esta es una bateria inicial, no cobertura exhaustiva: faltan HTTP/JWT/E2E,
carrera pago-financiacion, doble venta de stock/vehiculo, pagos de cuotas,
ultima cuota y movimientos de clientes compartidos. Se ampliaran antes de
declarar lista una entrega de integridad. La barrera de concurrencia es test-only;
la prueba de caracterizacion exige ambas llegadas y la de aceptacion permite
serializacion futura sin deadlock artificial. Errores SQL/timeout no se aceptan
como conflictos comerciales exitosos.

Advertencias de preparacion resueltas: la primera ejecucion SQL rechazo una
instancia detenida por inactividad, antes de crear base; el script resuelve el
arranque. No se instalaron SQL Server ni Docker: se uso LocalDB ya instalado.

La correspondencia con produccion sigue sin conocerse. Ninguna prueba se
ejecuto en produccion ni autoriza un despliegue. Fase 0.5 terminada: la Fase 1
requiere autorizacion nueva y debe preservar el aislamiento actual.
