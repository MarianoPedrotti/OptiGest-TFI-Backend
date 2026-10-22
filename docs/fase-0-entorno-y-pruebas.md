# OptiGest - Fase 0: entorno aislado y plan de pruebas

## Alcance y estado

Preparacion autorizada, sin implementar reglas ni transacciones. No se iniciaron
la API ni el frontend, no se accedio a bases de datos y no se ejecutaron
migraciones, commits, push o despliegues. Las suites descriptas abajo son una
propuesta: no fueron creadas ni ejecutadas.

Se aplicaron los AGENTS del proyecto general, backend y frontend y las skills
code-review, dotnet-backend, vue-frontend, ef-core-database y fullstack-feature.

## Versiones y worktrees

| Capa | Rama nueva | Commit base |
| --- | --- | --- |
| Backend | fix/stable-sales-financing-rules | 0450b9bf898863eb5308d240136ee8e7f4fe531c |
| Frontend | fix/stable-sales-financing-ui | 513ce535c3b056e6f70cf8cea3cf5655b0a62950 |

Rutas locales:

- Backend: `C:/Users/Pc/AppData/Local/Temp/opencode/optigest-stable-backend`.
- Frontend: `C:/Users/Pc/AppData/Local/Temp/opencode/optigest-stable-frontend`.
- App frontend: subdirectorio `frontend/` de ese worktree.

Antes de crear ambos worktrees se comprobo que no existian ramas locales ni
rutas registradas con estos destinos. Se uso `git worktree add -b` con cada
hash completo, sin copiar cambios del directorio original.

El backend original estaba en feature/multitenancy-central, commit
8d226e1989e788e391bdf3d99f1fa370af2dbd18, limpio. El frontend original estaba en
feauteResponsive, commit 619ec1842c94ded7c6641e70d37972fc8333c16f, con los 26
archivos pendientes ya conocidos. No se cambiaron sus ramas ni archivos.
El hash Git del patch frontend previo fue
`cf81690e5432d4432b427ba57ec86e580e3ed00b`; se usa para comparar al cierre.

Estas rutas estan en Temp: conservarlas durante el trabajo y no eliminarlas
mediante limpieza temporal. Cualquier traslado posterior requiere acordar una
ruta persistente y usar el mecanismo de worktrees de Git.

## Compilacion comprobada

- Backend: `dotnet build SistemaDeVentas.sln`, exitoso: 0 errores y 73
  advertencias CS8618/CS8602 de nullability en el snapshot.
- SDK disponibles: 7.0.317 y 9.0.305. El build net8.0 se realizo con SDK 9.0.305;
  runtime .NET/ASP.NET Core 8.0.20 disponible. No hay global.json versionado.
- Frontend: Node 22.16.0, npm 10.9.2; `npm ci` y `npm run build` exitosos.
- npm informo 6 vulnerabilidades de severidad alta en las dependencias actuales.
  No se ejecuto audit fix ni se actualizaron dependencias.
- Solo se restauraron dependencias existentes. No se instalaron frameworks de
  tests, SQL Server, Docker, browsers ni herramientas nuevas.
- Compilar no demuestra conectividad, funcionamiento comercial ni rollback.

## Procedencia de produccion: NO identificada

Se revisaron arboles Git de los snapshots, historial/tags disponibles,
Program.cs, csproj, launchSettings, manifiesto frontend, Vite, README y rutas
de workflows/metadatos de despliegue. No hay workflows de despliegue en los
snapshots, tags locales que identifiquen releases, ni un identificador de build
expuesto que permita vincular estos hashes con lo instalado.

El frontend usa version de paquete 0.0.0; eso no identifica un despliegue.
Los perfiles localhost del backend y VITE_API_URL=/api en produccion tampoco
identifican el commit ni el servidor actualmente publicado.

No se consultaron datos comerciales, infraestructura ni secretos productivos.
No se tomo publish*, deploy-somee o binarios locales como fuente de verdad.
Las carpetas docs generales disponibles tratan multitenancy y no se usaron para
deducir la version estable.

Pendiente: obtener del responsable del despliegue identificador del pipeline,
commit, fecha y hash/manifiesto del artefacto frontend y backend realmente
instalado. Si se facilitan artefactos sin secretos, comparar su procedencia.
Publicar un commit no demuestra que este desplegado. Despliegue bloqueado hasta
resolver la correspondencia y revisar diferencias relevantes.

## Testing realmente existente

- La solucion backend contiene un unico proyecto Web net8.0, sin proyectos ni
  paquetes xUnit/NUnit/MSTest/Test SDK.
- Controllers/TestController.cs contiene endpoints de autorizacion; no es una
  suite automatizada.
- Frontend solo configura dev, build y preview. No hay runner/configuracion
  versionada de Vitest, Jest, Cypress o Playwright ni scripts de test/lint.
- Playwright del harness esta disponible para inspeccion futura, pero no
  equivale a una suite reproducible dentro del repositorio.

## Revision previa de integridad

### Contexto compartido

Program.cs:31-36 registra AddDbContext<AppDbContext> sin cambiar su lifetime
scoped predeterminado; services y repositories financieros son AddScoped.
VentaRepository, ProductoRepository, VehiculoRepository, PagoVentaRepository,
FinanciacionRepository, CuotaRepository, PagoCuotaRepository y
CuentaCorrienteRepository reciben AppDbContext por constructor.

No se encontraron new AppDbContext, IDbContextFactory, scopes anidados ni
OnConfiguring que cambien esta conclusion en el codigo de aplicacion.
Dentro de una peticion/scope normal comparten instancia. Esto esta confirmado
por configuracion estatica, no por una prueba de ContextId en runtime.
Peticiones concurrentes deben tener scopes/contextos distintos.

### SaveChanges y limites de operacion

| Flujo | Secuencia actual de escrituras |
| --- | --- |
| VentaService.CrearAsync | Venta+detalles+pago inicial; luego cada stock/vehiculo; luego debito de venta y credito inicial si corresponde |
| FinanciacionService.CrearAsync | Financiacion; interes si positivo; una escritura por cuota |
| PagoVentaService.CrearAsync | Pago; movimiento de cuenta; actualizacion de venta |
| PagoCuotaService.CrearAsync | Pago; estado de cuota; financiacion/venta si ultima cuota; movimiento de cuenta |

Cada metodo repository llama SaveChangesAsync. No existe una transaccion
explicita global ni TransactionScope. Cada SaveChanges puede persistir TODO lo
pendiente en ese contexto, no solo la entidad que da nombre al repository.
Por ejemplo, un estado modificado en una entidad trackeada puede guardarse
desde el SaveChanges de otro repository. Las pruebas deben observar las tablas
reales y el ChangeTracker; no inferir limites a partir del nombre del metodo.

VentaRepository.ObtenerPorIdAsync usa AsNoTracking y AsSplitQuery; otros
repositories devuelven grafos trackeados. Las actualizaciones mediante Update
del grafo requieren pruebas de identity resolution y estados EF antes de
introducir bloqueos/recargas. No se afirma un bug adicional sin reproducirlo.

### Reintentos

UseSqlServer no configura EnableRetryOnFailure. No se encontraron estrategias
personalizadas, execution strategy explicita o interceptores de reintento en
la aplicacion. No hay replay transaccional configurado por el snapshot.
No se inspeccionaron opciones de conexion externas ni secretos; reintentos de
apertura de SqlClient no deben confundirse con repetir una venta completa.

Si posteriormente se habilita resiliencia EF, la estrategia debe envolver la
unidad completa de transaccion y cada intento debe usar un estado/contexto
coherente. No reintentar solo el SaveChanges que fallo.
Ante error debe revertirse la transaccion completa y descartarse el contexto;
un rollback de SQL no restablece automaticamente los objetos en memoria.
Los savepoints no reemplazan el rollback total de una operacion comercial.
MARS se verificara en la conexion de prueba, sin asumir la configuracion real.

### Concurrencia que debe coordinarse

- Venta vs venta por producto o vehiculo: no sobreventa ni disponibilidad doble.
- Financiacion vs pago directo: saldo actualizado y una decision serializada.
- Financiacion vs financiacion: mantener UNIQUE de VentaId y conflicto claro.
- Pago directo vs pago directo: no superar saldo ni perder cambios de estado.
- Pago de cuota vs mismo pago: no duplicar cobro ni credito en cuenta corriente.
- Ultimas cuotas concurrentes: cancelacion de financiacion/venta consistente.
- Movimientos de distintas ventas/cuotas del mismo cliente: saldo por cliente
  consistente, no solo bloqueo por venta.

CuentaCorrienteRepository.CrearAsync calcula Saldo desde SUM(Debe-Haber) y
guarda luego. La lectura y escritura necesitan coordinacion por cliente.
Todos los flujos deben seguir el mismo orden global de recursos que se defina
en Fase 1; no bloquear cada service con ordenes incompatibles.

PagoVentaService no rechaza explicitamente una venta con financiacion existente;
la UI oculta ese pago, pero una llamada directa o concurrente puede alcanzarlo.
Definir su tratamiento como parte del protocolo, sin cambiarlo en Fase 0.

### Errores, idempotencia y confirmacion

Controllers de venta/financiacion no traducen las Exception de reglas a un
contrato 4xx; no hay handler global de errores de negocio en Program.cs.
Preparar errores controlados para validacion, conflicto y fallo inesperado.
No exponer mensajes SQL ni secretos.

Una caida de red durante commit puede dejar el resultado desconocido. No se
debe repetir automaticamente una operacion comercial completa. UNIQUE por venta
no garantiza idempotencia de pagos o ventas. Si se requiere replay seguro de
estos comandos, acordar identificador de operacion y verificacion persistida;
eso puede ampliar contrato/esquema y requiere aprobacion especifica.
Para financiacion, consultar si ya existe y recuperar su resultado antes de
ofrecer repetir. Confirmar exito solo despues de commit.

## Infraestructura de pruebas propuesta (requiere autorizacion)

Hay SQLCMD y SqlLocalDB instalados. MSSQL$SQLEXPRESS figura en ejecucion;
no se conecto ni se reutilizo porque puede compartir otros trabajos.
Docker no fue encontrado en PATH. No se verifico version/configuracion del
motor ejecutando consultas y no se conoce el motor productivo exacto.

Preferencia local: provisionar una instancia LocalDB nueva y exclusiva y bases
de nombre unico OptiGest_Test_<runId>, con datos sinteticos desde cero.
Para CI, provisionar SQL Server aislado con version/configuracion equivalente
a produccion cuando se conozca; contenedores son una opcion, no un requisito
ya instalado. Se necesita permiso antes de crear instancias/bases o instalar.

Seguridad de ejecucion:

- El UserSecretsId del csproj es el mismo del proyecto original: el worktree
  NO aisla User Secrets ni variables de entorno de usuario/maquina.
- .env.development del frontend apunta a localhost:7049 y los launch profiles
  backend a 7049/5054; podrian conectar con la API de otra sesion.
- Crear host de tests con entorno Testing, fuentes de configuracion controladas,
  conexion exclusiva de prueba y JWT sintetico; no cargar secretos compartidos.
- Usar puertos/origenes dedicados y comprobados al iniciar, no reutilizar los
  perfiles actuales. No se asignaron puertos ni cambio configuracion externa.
- Fixture fail-closed: verificar servidor esperado, nombre de base, marcador de
  prueba y permisos acotados antes de cualquier escritura o limpieza.
- No restaurar backups productivos ni copiar credenciales, clientes o ventas.
- Acordar por separado creacion del esquema desde migraciones existentes y
  limpieza de bases descartables. Ninguna migracion fue aplicada en Fase 0.

## Plan concreto de suites (por crear tras autorizacion)

1. Unitarias net8.0 con xUnit/Test SDK/runner compatibles. Dobles de las
   interfaces repository para reglas de venta y financiacion, sin base real.
   Casos: un vehiculo o articulos, saldo positivo, rango inclusivo sobre saldo,
   duplicados agrupados, cantidades invalidas y ausencia de llamadas de escritura
   ante rechazo. No pretender demostrar rollback con mocks.
2. Integracion de services/repositories sobre SQL Server exclusivo con datos
   sinteticos. Luego pruebas HTTP con Microsoft.AspNetCore.Mvc.Testing net8.
   Proyectos sugeridos tests/SistemaDeVentas.UnitTests y
   tests/SistemaDeVentas.IntegrationTests. El proyecto Web esta en la raiz y
   compila **/*.cs: al agregar tests hay que excluir sus fuentes del csproj Web.
   La accesibilidad de Program para WebApplicationFactory tambien debe prepararse
   minimamente; no se cambio en Fase 0.
3. Fallos inyectados mediante interceptores EF de tests/decoradores controlados,
   sin hooks productivos: fallar despues de guardar financiacion, interes, cuota
   intermedia, venta, stock, pago o antes de terminar cuenta corriente.
4. Concurrencia con dos scopes, dos contextos y dos conexiones independientes;
   barreras deterministas y timeout, no compartir DbContext ni depender de sleeps.
   Cubrir todas las parejas del protocolo y verificar resultado serial valido.
5. Pruebas HTTP de conflicto, rollback, respuesta exitosa posterior al commit,
   error de lectura posterior y reconciliacion tras resultado incierto.

Para pruebas de transacciones reales no envolver toda la prueba en una
transaccion exterior: podria ocultar commits parciales o cambiar el comportamiento
de las transacciones de aplicacion. Usar bases/datasets aislados y limpieza
autorizada. Separar paralelismo de ejecucion de suites y concurrencia intencional.

### Oraculos de integridad

Leer siempre con contexto NUEVO despues del exito/fallo:

- Financiacion: cero nuevas cabeceras/cuotas/intereses tras fallo; tras exito,
  una financiacion y exactamente N cuotas cuya suma coincide con MontoTotal.
- Venta: tras fallo sin nuevas ventas, detalles, pagos ni movimientos y sin
  cambios de stock/vehiculo; tras exito, detalles normalizados y stock correcto.
- Pagos: pago, estado y credito contable ocurren juntos o ninguno ocurre.
- Cuenta: saldo total y secuencia de saldos persistidos coherentes por cliente.
- Concurrencia: no saldo negativo por sobrepago, stock negativo, doble venta del
  vehiculo ni doble pago/financiacion; errores controlados cuando haya conflicto.
- No exigir que identity no tenga huecos tras rollback: eso no es un fallo.

## Frontend y regresion coordinada

Ver docs/fase-0-entorno-y-pruebas.md del worktree frontend para el detalle.
Proponer Vitest + Vue Test Utils + entorno DOM para componentes/composables y
Pinia/router; Playwright Test para flujos reales con API de prueba y fixtures.
Nuevas dependencias/configuraciones quedan pendientes de autorizacion.

Matriz E2E: crear venta de vehiculo con/sin entrega, articulos consolidados,
rechazo mixto/multiples vehiculos, financiar solo vehiculo con saldo, limites de
plan, abrir cuotas por ID creado, refresco de tabla y orden descendente, cobro
directo/cuota, ultima cuota y cuenta corriente; preservar historicos sinteticos.
Viewports: 1366x768, 768x1024, 390x844 y 375x812.

## Gate de Fase 1

Preparado para iniciar trabajo aislado SOLO con nueva autorizacion. Pendientes:

- Autorizar paquetes/configuracion de tests y provisionamiento SQL exclusivo.
- Acordar esquema/datos sinteticos, puertos, configuracion Testing y limpieza.
- Definir protocolo unico de concurrencia e idempotencia antes de habilitar replay.
- Obtener pruebas de caracterizacion y fallos antes de dar integridad por validada.
- Resolver correspondencia con produccion antes de cualquier despliegue.

Fase 0 no autoriza implementar, aplicar migraciones ni publicar. No se crearon
commits. Los unicos archivos nuevos de trabajo son documentacion en docs/ de
los worktrees aislados; bin/obj/node_modules/dist son artefactos locales ignorados.

Referencias oficiales consultadas mediante Context7:
- https://learn.microsoft.com/ef/core/saving/transactions
- https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
- https://learn.microsoft.com/ef/core/testing/testing-with-the-database
- https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy
