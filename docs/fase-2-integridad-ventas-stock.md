# Fase 2 - Ventas atomicas, composicion y stock protegido

## Entorno y alcance

Worktree: `C:/Users/Pc/Desktop/OptiGest-Worktrees/backend-estable`.
Rama: `fix/stable-sales-financing-rules`.
Punto de partida verificado limpio: `dc980d09b2a454781d62ed4399d2fb2b1910d961`.
Se aplicaron los AGENTS y las skills code-review, dotnet-backend,
ef-core-database y fullstack-feature ya identificadas.

Solo se implementa backend de ventas. No se modifican frontend, entidades,
AppDbContext, DTO de requests, migraciones, dependencias, ordenamiento de
listados, cache, redireccion ni el codigo financiero de Fase 1. No se hacen
push, merge ni despliegues. No se alteran ventas historicas ni se usan bases
productivas. No hay cambio de arquitectura.

## Composicion y consolidacion

VentaService.CrearAsync toma un snapshot de cliente, detalles y pago inicial.
NormalizarDetalles valida todos los renglones antes de iniciar escrituras:

- Cliente y usuario con identificadores positivos.
- Detalles no vacios, sin elementos null.
- Exactamente uno entre ProductoId y VehiculoId por renglon, con id positivo.
- Cantidades enteras positivas; para vehiculo obligatoriamente uno.
- Si hay vehiculo, debe ser el unico detalle, sin articulos ni otro vehiculo,
  aunque sea un id repetido.
- Si hay articulos, se agrupa por ProductoId, se suma con checked(int) y se
  rechaza overflow. Las cantidades invalidas se rechazan ANTES de sumar: no
  pueden compensar cantidades positivas.
- Los detalles normalizados son copias: no se modifica el request original.

Dentro de la operacion protegida se verifican cliente/usuario, existencia de
items, disponibilidad de vehiculo, stock ACUMULADO y precios persistidos.
PrecioVenta debe ser positivo y representable como monto SQL decimal(18,2);
el total tampoco puede exceder ese rango. Los precios no vienen del request.
Pago inicial positivo requiere forma de pago, precision de dos decimales y no
puede superar el total; cero conserva el comportamiento sin pago inicial.
No se cambian los precios del catalogo ni importes historicos.

## Transaccion completa

Se reutiliza IOperacionFinancieraRepository.EjecutarAsync de Fase 1, sin cambiar
su implementacion. Todos los repositories usan el mismo AppDbContext scoped.
VentaService recibe ademas IUsuarioRepository (ya registrado en DI) y la
operacion compartida; no crea contextos propios.

Secuencia:
1. Transaccion READ COMMITTED y applock exclusivo del cliente, como en Fase 1.
2. Validacion y adquisicion de TODOS los recursos del carrito.
3. Calculo de total/pago/estado, antes de cualquier INSERT o UPDATE comercial.
4. Venta + detalles normalizados + pago inicial, guardados como grafo.
5. Descuento condicional de stock o transicion condicional del vehiculo.
6. Debito completo de venta y credito del pago inicial en cuenta corriente.
7. COMMIT antes de exito HTTP. Cualquier fallo previo revierte todos los pasos.

Estados conservados: Pendiente sin entrega, PagoParcial con entrega menor al
total, Pagada con pago completo. La respuesta existente informa ademas sus
flags y saldo inicial coherentes, sin agregar campos ni cambiar el request.
La creacion sigue respondiendo HTTP 200. VentaController usa el filtro de
errores existente: 400 composicion/datos, 404 recurso inexistente, 409 stock o
disponibilidad, 503 resultado de commit desconocido; message/code compatibles.

## Orden y proteccion de recursos

Orden global: cliente -> productos por Id ascendente, o cliente -> unico
vehiculo. Nunca se adquiere un segundo cliente ni se mezclan tipos de inventario.
Financiacion/pagos/cuotas solo requieren el cliente; por eso no introducen una
dependencia inversa inventario -> cliente. Se evitan ciclos de carritos
presentados en orden contrario, sin un lock global de catalogo o de ventas.

ProductoRepository.ObtenerParaVentaAsync y VehiculoRepository.ObtenerParaVentaAsync:
SELECT parametrizado por PK con UPDLOCK, HOLDLOCK, ROWLOCK, dentro de transaccion.
Los locks de actualizacion se mantienen hasta commit/rollback. El primer
comprador impide que otro valide contra las mismas existencias antiguas, aun
con clientes diferentes. Precios y disponibilidad tampoco pueden actualizarse
en esa fila hasta terminar la transaccion.

Las lecturas son AsNoTracking y NO cargan grafos de marca/modelo/categoria.
DescontarStockAsync usa ExecuteUpdateAsync con WHERE Stock >= cantidad;
MarcarVendidoAsync exige Estado == Disponible. Debe afectarse exactamente una
fila o se rechaza la operacion y se revierte su cabecera. Nunca se usa Update
de un grafo para modificar existencias. ExecuteUpdate no sincroniza tracking:
por eso no se trackean estas entidades ni se descuentan nuevamente en memoria.
Ambos accesos rechazan ejecutarse fuera de transaccion.

Clientes diferentes con inventario independiente pueden completar operaciones
mientras otra esta pausada. Las operaciones del mismo cliente siguen
serializadas por cuenta corriente, tal como exige Fase 1.

ROWLOCK es un hint, no una promesa absoluta contra escalacion bajo carritos
masivos/presion de memoria. No se impuso un limite comercial arbitrario al
carrito. Medir contencion/carga antes de desplegar; los conflictos SQL siguen
controlados por la infraestructura de Fase 1.
Ediciones administrativas con DTO de stock absoluto obsoleto o SQL externo
requieren su propio control de concurrencia; no se refactorizaron esos CRUD ni
se afirma que este protocolo repare cambios manuales posteriores al commit.

## Pruebas y resultados

Se verifico la linea base de Fase 1: 69 exitosas antes de modificar codigo.
Tras la implementacion inicial: 74 activas exitosas.
Comprobaciones puntuales posteriores: 24 unitarias nuevas, 23 SQL iniciales de
atomicidad y 10 SQL de concurrencia/HTTP exitosas. Se agregaron luego otros
3 casos SQL para validar por completo el carrito antes de escribir.

Suite final normal: **134 exitosas, 0 fallidas, 0 omitidas**:
69 casos unitarios y 65 casos SQL/HTTP sobre LocalDB exclusiva de Testing.
Suite PendingRule: **2 fallidas conocidas**, solamente orden de ventas y
financiaciones; se conservaron porque el usuario excluyo ordenamiento.
Build final con --no-incremental: **correcto, 0 errores y 73 advertencias
nullable preexistentes**. git diff --check no reporto errores.

Transicion explicita: se retira
Characterization_current_sale_leaves_sale_payment_stock_and_debit, porque exigia
el defecto corregido. Se activa Required_sale_failure_rolls_back_every_write
sin cambiar su expectativa (0 ventas/pagos nuevos, stock 10 y saldo sin delta).
Se activan las 5 pruebas unitarias pendientes de mezcla, multiples vehiculos,
consolidacion y stock acumulado sin debilitar sus aserciones. Los otros
68 casos que estaban activos en Fase 1 siguen pasando.

Evidencia verificada con contextos y conexiones independientes:
- Un vehiculo disponible, cantidad uno, precio real y estado vendido.
- Articulos multiples/consolidados: un renglon por producto, precios y totales
  persistidos, stock exacto y estados para pago cero/parcial/completo.
- Rechazos sin efectos: mezcla, vehiculos distintos/repetidos, cantidad de
  vehiculo distinta de uno, ids incompatibles/ausentes, cantidades negativas,
  overflow acumulado, falta de referencias, precio invalido y falta de stock.
- Un ultimo item invalido no persiste cabecera ni descuenta el primero valido.
- Fallos inyectados DESPUES del SaveChanges de cabecera, antes/despues de UPDATE
  de stock (primer y segundo producto), debito y credito contable: rollback de
  venta, detalles, pago, ambas existencias y cuenta corriente.
- Fallo despues del UPDATE real del vehiculo: vuelve a Disponible y no queda venta.
- Dos clientes por el mismo producto: uno vende 6 y el otro es rechazado,
  stock final 4; si la suma es 10, ambos venden y stock final cero.
- Dos usuarios/clientes por el mismo vehiculo: una venta y un debito; el otro
  recibe conflicto y no deja cabecera ni movimientos.
- Se observan locks KEY U/X concedidos en sys.dm_tran_locks de la base Testing
  mientras la venta esta pausada antes de cabecera. No son mocks de transacciones.
- Carritos en orden contrario: ambos completan y el observador SQL registra las
  claves ascendentes en cada contexto, sin deadlock evitable en ese escenario.
- Una venta con cliente/producto independiente completa antes de liberar la
  primera; productos distintos del mismo cliente conservan saldos acumulados.
- Venta concurrente con pago/financiacion de otra venta del mismo cliente:
  saldos/prefijos contables coherentes y regresion financiera intacta.
- HTTP real en TestServer aislado: precios enviados no se aceptan como fuente,
  consolidacion ya confirmada al responder; composicion 400 y existencias 409.
- Perdida simulada de confirmacion DESPUES del commit real: un solo agregado
  completo, sin replay. Esto no agrega idempotency keys a ventas/pagos.

Comandos, desde la raiz del worktree backend:

```powershell
dotnet build SistemaDeVentas.sln --no-incremental --verbosity quiet
powershell -NoProfile -File scripts/Test-Testing.ps1
powershell -NoProfile -File scripts/Test-Testing.ps1 -PendingRules
```

La instancia es solo `(localdb)\OptiGest_Testing_Phase05`; cada ejecucion usa
una base NUEVA OptiGest_Testing_Phase05_<guid> con marcador de propiedad.
Datos sinteticos, sin User Secrets, JWT productivo, puertos ni conexiones
comerciales. Se reutilizan las migraciones existentes solo para ese esquema
Testing. Los TRX y datos locales se mantienen ignorados, no forman parte del commit.

## Revision y limites finales

No se modificaron metodos de lectura/orden de ventas, financiaciones o pagos.
No se cambiaron entidades, DTO, configuracion, paquetes ni el ejecutor de
Fase 1. HTTP de pruebas reutiliza el host seguro de Fase 1 con DI adicional
para VentaService. No se modifico ningun archivo frontend.
No se arreglo ni migro ningun dato historico.

Revision final del diff y de los casos nuevos: no se encontraron hallazgos
significativos en el alcance revisado. Se verifico que backend original sigue
limpio en feature/multitenancy-central (8d226e1), su main sigue en 0450b9b y el
frontend estable sigue limpio en a2ca09d. El frontend original conserva sus
26 archivos modificados y main/feauteResponsive en 619ec18; su patch binario
sigue con hash Git cf81690e5432d4432b427ba57ec86e580e3ed00b.
La comparacion usa pipeline binario mediante cmd para evitar recodificacion
de texto por Windows PowerShell.

Persisten advertencias nullable originales y la falta de identificacion exacta
de los despliegues productivos. No es una autorizacion de despliegue.
El lock por cliente, las escrituras y SQL manuales ajenos, los timeouts bajo
carga y la reconciliacion de resultados desconocidos siguen requiriendo las
precauciones documentadas en Fase 1. No hay reintentos automaticos.
Se termina Fase 2 y se espera revision antes de UI, cache, redireccion u orden.

Referencias oficiales consultadas mediante Context7: SQL Server table hints
UPDLOCK/HOLDLOCK/ROWLOCK y EF Core ExecuteUpdate/transacciones/tracking.
