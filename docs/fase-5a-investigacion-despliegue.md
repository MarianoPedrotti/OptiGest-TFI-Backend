# Fase 5A — Investigación local de despliegue (solo lectura)

## Evidencia revisada

Árboles Git de ambos snapshots estables, tags locales, historial por rutas de
publicación, nombres de archivos en Properties, documentación estable, README
frontend, configuración pública `VITE_API_URL`, Program y configuración EF del código.
En el directorio original se listaron solamente nombres de carpetas/metadatos:
`publish/`, `publish-somee/`, `deploy-somee/`, `wwwroot/`, Properties y `.github/workflows/`.
La carpeta de workflows original está vacía. Sus archivos de configuración publicados,
User Secrets y credenciales no se abrieron. No se hicieron consultas a infraestructura remota.

## Resultados

| Tema | Resultado |
| --- | --- |
| Frontend | Vite produce `dist/`; `.env.production` declara `VITE_API_URL=/api`. README de plantilla, sin procedimiento operativo de publicación. |
| Backend | API net8.0. El código estable soporta archivos estáticos y fallback SPA desde wwwroot; es compatible con servir el build frontend desde el mismo origen. |
| Mecanismo efectivamente usado en producción | No demostrado por la documentación local. Los nombres de carpetas `somee` son indicios históricos, no prueba del hosting actual. |
| Commits desplegados | No identificados. No hay tags de release ni manifiesto/hash que vincule un artefacto instalado con estos commits. `package.json` 0.0.0 no identifica una publicación. |
| Pipeline | Ningún workflow de despliegue versionado en los snapshots revisados. |
| Base de datos | El código usa EF Core SQL Server y la clave `ConnectionStrings:DefaultConnection`. Destino, versión del motor, permisos y valor efectivo de producción no comprobados; no se leyeron secretos. |
| Migraciones | Dos migraciones en el snapshot estable. Ambas aplicadas a la base sintética y ninguna pendiente allí. Estado de migraciones de producción desconocido. |
| Backup/restauración | No se encontró un procedimiento operativo verificable para el despliegue estable ni evidencia de restauración ensayada. |
| Rollback de despliegue | No se encontró procedimiento ni artefacto identificado para volver a la versión instalada. Los tests de rollback transaccional no prueban rollback de publicación. |
| Monitoreo | Sin evidencia local de alertas, monitoreo operativo o comprobaciones posteriores a publicación configuradas para el entorno desplegado. |

La documentación general adicional encontrada corresponde a multitenancy y no se
utilizó para atribuir versión ni configuración al despliegue estable.

## Bloqueos para Fase 5B

1. Obtener del responsable de producción commits/manifiestos/hashes y fecha de los
   artefactos realmente instalados de ambas capas.
2. Obtener evidencia sanitizada del entorno y esquema efectivo, incluyendo historial
   de migraciones, versión de SQL Server y permisos requeridos por `sp_getapplock`.
3. Identificar procedimiento de publicación y responsables, backup restaurable,
   mecanismo de rollback y monitoreo posteriores.
4. Resolver y revalidar los hallazgos funcionales de Fase 5A antes de aprobar publicación.

Este relevamiento no preparó artefactos de release ni ejecutó ninguna acción de Fase 5B.
