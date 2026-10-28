# multicanalprueba

Evaluación técnica e-Solutions sobre la **plantilla ASP.NET Web Forms (VB.NET / .NET Framework 4.8)** de Visual Studio Community.

Se conservan los archivos base de la plantilla (`App_Start`, `Content`, `Scripts`, `Site.Master`, `Default`/`About`/`Contact`, `packages.config`, etc.) y se agrega la lógica multi-canal encima.

## Requisitos

- Visual Studio 2026 Community (u otra con workload ASP.NET / .NET 4.8)
- SQL Server Express (`.\SQLEXPRESS`)

## 1. Base de datos

```bat
sqlcmd -S .\SQLEXPRESS -E -i sql\01_configuracion_canal.sql
```

## 2. Ejecutar el sitio

1. Abrir `multicanalprueba.slnx` (o el `.vbproj`) en Visual Studio.
2. F5 (IIS Express).
3. Probar:
   - `/Empresas?canal=AB` — listado + menú
   - `/Empresas?canal=CJ` — listado **sin** menú
   - Seleccionar empresa → `Detalle` (Session)
   - `/Detalle?canal=AB` sin sesión → vuelve al listado

## 3. Servicio simulado

Handler en el **mismo** proyecto: `GET /api/empresas?canal={canal}` + header `X-Api-Key`.

- Keys: `key-canal-ab-demo` / `key-canal-cj-demo` (`Web.config` appSettings)
- `?delayMs=4000` simula demora
- `?forceError=1` simula error 500

La URL del servicio sale de SQL (`/api/empresas`); el cliente la resuelve contra el host actual.

## Agregar un canal sin tocar código de páginas

```sql
INSERT INTO dbo.ConfiguracionCanal
  (Canal, UrlServicio, ApiKey, MostrarMenu, PaginaDestino, TimeoutSegundos, Activo)
VALUES
  ('XY', N'/api/empresas', N'key-canal-xy-demo', 1, N'Detalle.aspx', 5, 1);
```

Y en `Web.config`:

```xml
<add key="ApiKey.XY" value="key-canal-xy-demo" />
```

## Estructura (plantilla + nuevo)

```
App_Start/          ← plantilla (BundleConfig, RouteConfig)
Content/, Scripts/  ← plantilla
Site.Master         ← plantilla adaptada (menú por canal)
Default/About/Contact
Models/, Data/, Services/, Infrastructure/   ← lógica de la prueba
Empresas.aspx, Detalle.aspx
EmpresasHandler.ashx (+ mapeo /api/empresas)
sql/
docs/Parte-A-Teorica.md
```

## Parte A

Las respuestas teóricas van en un documento aparte (no versionado en este repo). Entregar PDF/Word o README según indiquen.

## Configuración local

`Web.config` usa un placeholder de connection string. Ajustá `Data Source` a tu SQL local (ej. `.\\SQLEXPRESS`). Referencia: `Web.config.example`.
