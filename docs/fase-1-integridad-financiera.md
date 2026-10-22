# Fase 1 - Integridad financiera del backend estable

## Bases y recuperacion

Backend: fix/stable-sales-financing-rules, base historica
0450b9bf898863eb5308d240136ee8e7f4fe531c. Infraestructura guardada en
ca9294017b663a6788790986ecf683c3ce503363.
Frontend: fix/stable-sales-financing-ui, base responsive
513ce535c3b056e6f70cf8cea3cf5655b0a62950; infraestructura y lockfile verificado
guardados en a2ca09d5cb41739ecae4261b27f586550e7fb3ff.
Rutas permanentes bajo C:/Users/Pc/Desktop/OptiGest-Worktrees/.

No se modificaron directorios originales, multitenancy, main ni produccion.
No se hizo push, merge ni despliegue. No se genero ninguna migracion ni cambio
de entidades/esquema. Solo se aplicaron las migraciones existentes a bases
Testing nuevas con marcador, como en Fase 0.5.

## Cambios funcionales

- FinanciacionService.CrearAsync: valida ids, descubre el cliente mediante query
  escalar, inicia la operacion financiera y vuelve a cargar la venta protegida.
  Exige un detalle de vehiculo, cantidad 1, sin ProductoId; venta no pagada,
  sin financiacion previa, saldo positivo y plan existente/activo. Minimo y maximo
  se comparan con Total - SUM(PagoVenta.Monto), incluidos pagos iniciales.
  No se cambian tasas, coeficiente, redondeo AwayFromZero, monto total ni formula
  que hace absorber diferencias a la ultima cuota. Se validan antes de guardar
  cantidades/tasas invalidas, vencimientos no representables y cuotas no positivas
  a precision monetaria; no se introduce un limite comercial arbitrario.
- Financiacion, interes y todas las cuotas se confirman juntos.
- PagoVentaService.CrearAsync: pago, credito contable y estado se confirman juntos.
  Rechaza financiacion existente incluso Cancelada; conserva pagos anteriores.
  Valida monto positivo representable a dos decimales y forma de pago. Mantiene
  el estado parcial original de este flujo (Parcial), sin migrar estados historicos.
- PagoCuotaService.CrearAsync: pago, cuota, eventual cancelacion de financiacion,
  venta pagada y movimiento se confirman juntos. Sigue exigiendo monto exacto,
  sin pagos parciales, y rechaza cuotas ya pagadas.
- Actualizaciones de estados especificas en Venta/Cuota/FinanciacionRepository:
  no se marca Update un grafo desconectado completo para cambiar un solo estado.
- CuentaCorrienteRepository.CrearAsync participa en la transaccion financiera
  existente o abre su propia operacion protegida cuando la llama un flujo legado.
  Saldo sigue siendo SUM(Debe-Haber) previo + Debe - Haber. Resumen y contador
  toman el ultimo movimiento por Id, no una fecha posiblemente anterior al orden
  real de insercion. No se reescriben movimientos historicos.

## Transaccion y protocolo SQL Server

OperacionFinancieraRepository, inyectado scoped por interfaz, utiliza el mismo
AppDbContext scoped que los repositories. Las reglas quedan en services y el
acceso SQL/transaccion en repository, sin nueva arquitectura general.

Cada operacion:

1. Rechaza transacciones exteriores y cambios previos sin guardar.
2. Rechaza una estrategia EF que habilite replay automatico de escrituras.
3. Abre transaccion READ COMMITTED.
4. Adquiere sys.sp_getapplock Exclusive, LockOwner=Transaction, sobre
   OptiGest:Cliente:<id>, principal SQL public, timeout 10000 ms.
5. Limpia solo snapshots trackeados sin cambios y vuelve a consultar el estado
   financiero. Los ids consultados antes sirven para elegir el recurso, no para
   decidir el saldo; se verifica que la pertenencia al cliente siga coincidiendo.
6. Ejecuta reglas y escrituras con los SaveChanges actuales dentro de la misma
   transaccion; COMMIT antes de devolver exito. Fallo previo -> ROLLBACK completo
   y limpieza de ChangeTracker; nunca continuar usando objetos modificados tras fallo.

Un cliente por operacion; un segundo cliente en la misma transaccion se rechaza.
Al compartir el recurso por cliente se serializan tanto operaciones de la misma
venta/cuota como movimientos de distintas ventas que comparten cuenta corriente.
Clientes diferentes tienen recursos distintos. La liberacion es automatica al
commit/rollback; no hay locks de sesion ni semaforos locales al proceso.

READ COMMITTED solo no seria suficiente: es el bloqueo de aplicacion compartido
por TODOS estos escritores el que protege la lectura-validacion-escritura.
La unicidad de Financiaciones.VentaId existente sigue siendo defensa de base.
No se cambian datos/montos historicos. El alcance es esta version cooperativa:
SQL manual, otras versiones o escritores que ignoren el protocolo no quedan
protegidos automaticamente por applock. Coordinar el despliegue de todos los escritores.

Timeout/cancelacion/victima de applock -> conflicto controlado. SQL 1205 es
victima de deadlock ya revertida por SQL Server y no intenta rollback duplicado;
1222 requiere rollback. No se reejecutan operaciones automaticamente.
Una excepcion al confirmar devuelve resultado_no_confirmado (503), sin afirmar
que se revirtio ni repetir la escritura; consultar el estado para reconciliar.

## HTTP y compatibilidad

FinancialExceptionFilter se aplica solo a FinanciacionController,
PagoVentaController y PagoCuotaController. Respuesta de error:
`{ message, code }`. Se conserva message, que entiende el frontend actual.
400 datos invalidos; 404 recurso inexistente; 409 regla/conflicto; 503 resultado
de commit desconocido; 500 mensaje generico para errores inesperados, con log
interno. No se devuelven errores SQL. Exito mantiene HTTP 200 y DTO existentes.
No se agregaron campos requeridos ni se cambiaron rutas o contratos request.

El servidor siempre calcula el saldo protegido. Esta fase no agrega al request
un monto esperado por el usuario: la reconfirmacion de una previsualizacion UI
desactualizada requiere un cambio de contrato/UI separado, no se simula que exista.

## Pruebas y transicion desde Fase 0.5

Comandos desde backend-estable:

```powershell
dotnet build SistemaDeVentas.sln
powershell -NoProfile -File scripts/Test-Testing.ps1
powershell -NoProfile -File scripts/Test-Testing.ps1 -PendingRules
```

Resultado normal: 69 exitosas (40 unitarias y 29 SQL/HTTP), sin fallidas ni omitidas.
Pendientes: 8 fallidas conocidas, todas fuera de Fase 1: 5 casos de composicion,
consolidacion y stock; rollback de creacion de venta; orden de ventas y financiaciones.
Frontend sin cambios funcionales: 13 caracterizaciones exitosas y 8 pendientes
iguales a Fase 0.5. No se reescribieron aserciones para aceptar persistencia parcial.

Transiciones explicitas:
- Se retira Characterization_current_financing_leaves_partial_persistence:
  exigia el defecto anterior. Se activa sin debilitar Required_financing_failure_rolls_back_every_write.
- Se retira Characterization_two_requests_can_overpay_the_same_sale:
  exigia 1600 pagados sobre 1000. Se activa Required_concurrent_payments_must_not_exceed_sale_total.
- Se activan las dos reglas unitarias de financiacion antes PendingRule.
- Se conserva la caracterizacion del fallo parcial de VentaService porque Fase 2
  no esta implementada; no se presenta como aceptacion de integridad de ventas.

Evidencias SQL (contextos nuevos despues de cada resultado):
- Fallo de interes y cuota intermedia: ningun nuevo registro de financiacion,
  cuotas o interes queda persistido. Reintento explicito tras rollback crea una vez.
- Dos financiaciones concurrentes: una cabecera, N cuotas, un interes; otra 409.
- Dos pagos 800 sobre total 1000: solo un pago/credito; el otro excede saldo.
- Pago contra financiacion: si gana pago, principal descuenta ese pago; si gana
  financiacion, pago directo rechazado. Nunca principal viejo mas pago posterior.
- Pago directo con financiacion activa o cancelada: rechazo sin efectos, pagos
  previos conservados. Fallo al acreditar pago directo revierte pago y estado.
- Pago de cuota: fallos en pago, cuota, financiacion, venta y movimiento revierten
  todo. Doble pago concurrente genera un pago/credito. Ultimas cuotas concurrentes
  dejan financiacion Cancelada, venta Pagada, tres pagos exactos y saldo cero.
- Distintas ventas del mismo cliente: cada Saldo coincide con la suma acumulada;
  resumen final coincide aunque una fecha del seed no siga el orden de insercion.
- Cliente diferente no bloqueado por lock ajeno; timeout controlado y reintento
  manual tras liberar crea un solo pago. Deadlock SQL real devuelve un conflicto,
  sin replay ni error de commit desconocido.
- Interceptor de tests falla DESPUES del commit real: se informa 503; existe un
  solo plan completo; otro POST encuentra la financiacion y no duplica.
- Configurar EnableRetryOnFailure se rechaza antes de ejecutar el delegado.
- TestServer HTTP real, sin puertos: 400/404/409 y exito cuyo plan ya es visible
  desde otro contexto. Autenticacion sintetica, sin validar JWT productivo.

Se agrego Microsoft.AspNetCore.TestHost 8.0.8 exclusivamente al proyecto de tests.
No se cambiaron paquetes productivos backend, AppDbContext ni migraciones.
Las bases nuevas son OptiGest_Testing_Phase05_<guid> en la instancia exclusiva;
se conservan para inspeccion y sus resultados TRX estan ignorados por Git.

## Limites y siguiente fase

- No se implemento composicion ni atomicidad global de creacion de venta/stock/
  pago inicial: VentaService no cambio. Sus movimientos contables individuales
  participan del lock, pero eso NO vuelve atomico el flujo legado completo.
- No se implementaron UI, redireccion, refresco, orden de listados ni cambios de
  mapeos de lectura ajenos a estos saldos de cuenta. Siguen registrados como pendientes.
- No hay idempotency key persistida para ventas/pagos: ante resultado incierto
  no repetir ciegamente; reconciliar primero. Una garantia de exactamente-una-vez
  requeriria contrato/persistencia adicionales y otra autorizacion.
- Los locks serializan por cliente; medir contencion y timeouts bajo carga realista.
- Confirmar permisos de sp_getapplock y version/schema del despliegue antes de
  publicar. No se identifico todavia la correspondencia con los artefactos productivos.
- Persisten advertencias nullable y vulnerabilidades originales de dependencias;
  no se aplicaron actualizaciones productivas por conveniencia.
- No se repararon automaticamente datos historicos que pudieran ser inconsistentes.

Fase 1 lista para revision local y para comenzar Fase 2 con nueva autorizacion,
no para despliegue automatico. Referencias consultadas: documentacion oficial
EF Core transactions/connection resiliency y SQL Server sys.sp_getapplock.
