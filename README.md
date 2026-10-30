# Multicanal — sitio de pagos por canal

Hola. En este repo dejé la solución de la evaluación práctica, un sitio con **ASP.NET Web Forms** en **VB.NET** utilizando .NET Framework 4.8 que lista empresas de pago según el **canal** que viene en la URL.

Incluí dos canales de prueba, **AB** y **CJ**, con comportamientos distintos sobre todo el menú, para demostrar que la misma página sirve a varios canales sin duplicar pantallas.

---

## Qué problema resuelve

En un sitio compartido por varios canales de pago, cada uno puede tener:

- otra URL de servicio  
- otra API key  
- menú visible o no  
- otra página de destino al elegir una empresa  
- otro timeout  

Yo modelé eso en una tabla de configuración y un procedimiento almacenado. La página pregunta: “¿qué canal es?” → trae la config → consume el servicio con **esa** URL y **esa** key → muestra el listado.

---

## Arquitectura 

Diagrama interactivo: [ver en GitHub Pages](https://jona2022-02.github.io/multicanalprueba/architecture.html) · fuente en [`docs/architecture.html`](docs/architecture.html).

Pensé el flujo en tres capas claras:

```
[ Navegador ]
      │
      ▼
[ Web Forms: Empresas.aspx / Detalle.aspx / Site.Master ]
      │  lee canal (?canal=AB)
      ▼
[ Data: CanalConfigRepository ] ── ADO.NET + SP ──► [ SQL Server: ConfiguracionCanal ]
      │
      │  UrlServicio + ApiKey + Timeout (desde BD)
      ▼
[ Services: EmpresasApiClient ] ── HttpClient ──► [ Handler: GET /api/empresas ]
                                                      (servicio simulado en el mismo sitio)
```

### 1. Presentación 
- **`Empresas.aspx`**: listado en mosaico con un `Repeater`. Cargo datos solo cuando **no** es postback; el consumo del API lo hice con **`RegisterAsyncTask`** para no bloquear el hilo a lo bruto.
- **`Detalle.aspx`**: muestra la empresa que guardé en **Session**. Si no hay sesión (o expiró), redirijo otra vez al listado.
- **`Site.Master`**: el menú se muestra u oculta según `MostrarMenu` de la config. Misma master, sin una página distinta por canal.

### 2. Datos
- Script en `sql/01_configuracion_canal.sql`: crea la BD (si hace falta), la tabla, el SP `usp_ConfiguracionCanal_Obtener` y el seed de AB/CJ.
- **`CanalConfigRepository`**: llama al SP con **parámetros** y `Using` en la conexión. Si el canal no existe, la página muestra un mensaje controlado.

### 3. Servicio simulado
- No levanté un proyecto aparte: armé un **handler** (`.ashx`) expuesto como `GET /api/empresas?canal={canal}` con header **`X-Api-Key`**.
- Sirve para simular el backend real de empresas. Soporta `delayMs` (demora) y `forceError=1` (error 500), así puedo probar timeout y fallos.
- La URL en SQL es relativa (`/api/empresas`); el cliente la resuelve contra el host donde corre el sitio.

### 4. Errores y log
- Al usuario solo le muestro mensajes claros para el usuario.
- El detalle técnico lo escribo en `App_Data/Logs/multicanal-AAAAMMDD.log` cuando falla el consumo del servicio (por ejemplo API key inválida o timeout).

### Carpetas importantes

```
Models/           → CanalConfig, EmpresaPago
Data/             → acceso a SQL (repositorio)
Services/         → cliente HTTP del listado de empresas
Infrastructure/   → logger y claves de Session
Empresas.aspx     → listado
Detalle.aspx      → empresa seleccionada
EmpresasHandler   → API simulada /api/empresas
sql/              → script de base de datos
```

---

## Cómo se corre

### Lo que utilice
- Visual Studio con soporte ASP.NET / .NET Framework 4.8  
- SQL Server Express  
- Ajustar el connection string en `Web.config` (en el repo va un placeholder; hay un `Web.config.example` de referencia)

### 1) Base de datos
Ejecuto el script:

```bat
sqlcmd -S .\SQLEXPRESS -E -i sql\01_configuracion_canal.sql
```

(Si tu instancia no es `.\SQLEXPRESS`, cambio el servidor en el comando y en `Web.config`.)

### 2) Sitio
Abro la solución en Visual Studio y lo ejecuto con (IIS Express).

Pruebas que hago yo:

| URL | Qué espero |
|-----|------------|
| `/Empresas?canal=AB` | Listado + **menú visible** |
| `/Empresas?canal=CJ` | Listado **sin** menú |
| Clic en **Seleccionar** | Guarda en Session y va a Detalle |
| `/Detalle?canal=AB` sin haber seleccionado | Vuelve al listado |
| `/Empresas?canal=XX` | Mensaje de canal inexistente |

### 3) Probar el API a mano (Postman / Thunder Client / curl)

```
GET https://localhost:<puerto>/api/empresas?canal=AB
Header: X-Api-Key = key-canal-ab-demo
```

- Key mala → **401**  
- `?delayMs=4000` → demora  
- `?forceError=1` → **500**  

Importante: el **log de la aplicación** se escribe cuando falla **la página** al consumir el servicio, no cuando llamo el handler directo desde Postman.

---

## Cómo agregaría un canal nuevo (sin tocar la lógica de las páginas)

Solo datos:

```sql
INSERT INTO dbo.ConfiguracionCanal
  (Canal, UrlServicio, ApiKey, MostrarMenu, PaginaDestino, TimeoutSegundos, Activo)
VALUES
  ('XY', N'/api/empresas', N'key-canal-xy-demo', 1, N'Detalle.aspx', 5, 1);
```

Y la key del simulador en `Web.config` (`appSettings`):

```xml
<add key="ApiKey.XY" value="key-canal-xy-demo" />
```

Después pruebo `/Empresas?canal=XY`. No necesito duplicar `Empresas.aspx` ni meter `If` por canal.

---

## Parte A — preguntas teóricas (mis respuestas)

Acá dejo, en primera persona, lo que yo respondería en la parte teórica. Traté de ser breve y con criterio.

### 1. Ciclo de vida de una página Web Forms. ¿Dónde cargo datos y qué pasa si no miro `IsPostBack`?

Yo entiendo el ciclo típico así: **Init** → **Load** → eventos de controles → **PreRender** → **Render** (y después Unload).

Cargo el listado en **Load**, y solo cuando **no** es postback (`Not IsPostBack`). Si en cada click del `Repeater` volviera a llamar al servicio, gastaría de más, podría pisar el estado de los controles y mezclar mal el ViewState.

Si no verifico `IsPostBack`, en cada postback se me re-ejecuta la carga completa: más costo y comportamientos raros (selecciones que “saltan”, datos que se pisan, etc.).

### 2. ViewState vs Session vs query string

- **ViewState:** yo lo uso para el estado de la **misma página** entre postbacks. Sé que infla el HTML y que, si se debilita el MAC, se puede manipular; por eso nunca metería secretos ahí.  
- **Session:** yo la uso para pasar datos **entre páginas** (acá, la empresa elegida). En InProc se me puede perder con un recycle del AppPool o si hay varios servidores sin estado compartido; tampoco guardaría API keys en Session.  
- **Query string:** yo la uso para navegación visible, como `canal=AB`. Cualquiera la ve y la puede cambiar, así que solo pondría códigos/ids, nada sensible.

### 3. Problemas de sesión (varios servidores, AppPool, iframe, Safari)

- **Varios servidores:** con InProc cada máquina tiene su propia memoria. Yo movería la sesión a StateServer o SQLServer, o pediría sticky sessions en el balanceador.  
- **Reciclaje del AppPool:** se me pierde la Session InProc. Por eso en Detalle, si no hay empresa en sesión, redirijo al listado en lugar de mostrar un error feo.  
- **Iframe de otro dominio / Safari (iOS):** las cookies de terceros y `SameSite` me pueden cortar la cookie de sesión. Yo preferiría same-site; `SameSite=None; Secure` solo con HTTPS y si no hay de otra, o evitar depender de cookies cross-site.

### 4. Consumir API REST desde .NET Framework (HttpClient, TLS, timeouts, `.Result`)

- Yo reutilizo **un HttpClient** compartido para no saturar sockets.  
- Fuerzo **TLS 1.2** con `ServicePointManager.SecurityProtocol` porque en Framework a veces hace falta.  
- El **timeout** lo tomo de la configuración del canal.  
- Evito `.Result` / `.Wait()` en el hilo de la página: me pueden generar **deadlock** con el SynchronizationContext. En el listado usé **`RegisterAsyncTask`**. Si en algún punto tuviera que ir síncrono, usaría `ConfigureAwait(False).GetAwaiter().GetResult()` con cuidado, no como costumbre.

### 5. Invocar un SP con ADO.NET de forma segura

Yo lo haría así:

- `CommandType.StoredProcedure`.  
- Parámetros tipados (`SqlParameter`); **nunca** concatenaría SQL a mano.  
- `Using` en conexión, comando y reader para liberar bien.  
- Transacciones (`SqlTransaction` / `TransactionScope`) cuando tenga varias escrituras que deban ir juntas. En esta prueba yo solo leo config con un SP puntual.

### 6. Agregar un canal nuevo sin afectar los que ya están en producción

Yo parametrizaría en BD (URL, key, menú, destino, timeout). El código solo consulta por canal; **no** agregaría `If canal = "XX"`.  
Para probarlo: inserto la fila del canal nuevo, configuro su API key en el simulador, y hago smoke test de listado + menú + Session → Detalle, sin redeploy de la lógica de las páginas.

### 7. Manejo de errores

En los bordes (BD y HTTP) yo uso `Try/Catch`. Al usuario le muestro un mensaje controlado. El detalle técnico (exception, status) lo mando al **log** en `App_Data/Logs`. Con `customErrors` evito tirar stack traces a producción.

### 8. Seguridad: XSS, inyección SQL, ViewState y qué no va en Session

- **XSS:** yo uso `Server.HtmlEncode` en lo que muestro; no renderizo HTML crudo de terceros; confío en `requestValidation` como capa extra.  
- **SQL injection:** solo parámetros / procedimientos almacenados; no armo SQL concatenando strings.  
- **ViewState:** dejo el MAC activo y no guardo secretos ahí.  
- **Session:** no guardo API keys, passwords ni datos muy sensibles; solo lo del flujo (empresa seleccionada y canal).

---



1. **Parametrizar por BD**, no por código, para no romper canales en producción al sumar uno nuevo.  
2. **Session** solo para la empresa elegida (y el canal), no para secretos como la API key.  
3. **Handler en el mismo sitio** para cumplir el servicio simulado sin complicar el arranque.  
4. **Mensajes amigables + log** cuando el servicio falla (401, timeout, etc.).

