# Multicanal — sitio de pagos por canal

Hola. En este repo dejé la solución de la evaluación práctica: un sitio **ASP.NET Web Forms** en **VB.NET** (.NET Framework 4.8) que lista empresas de pago según el **canal** que viene en la URL. La idea que seguí es simple: **nada de reglas hardcodeadas por canal** (nada de `If canal = "AB"`). Lo que cambia entre canales vive en **SQL Server**, y el código solo lee esa configuración.

Incluí dos canales de prueba, **AB** y **CJ**, con comportamientos distintos (sobre todo el menú), para demostrar que la misma página sirve a varios canales sin duplicar pantallas.

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

## Arquitectura (cómo lo armé)

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

### 1. Presentación (Web Forms)
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
- Al usuario solo le muestro mensajes claros (sin stack trace).
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

## Cómo lo corro en mi máquina

### Lo que necesito
- Visual Studio con soporte ASP.NET / .NET Framework 4.8  
- SQL Server Express (o el servidor que uses)  
- Ajustar el connection string en `Web.config` (en el repo va un placeholder; hay un `Web.config.example` de referencia)

### 1) Base de datos
Ejecuto el script:

```bat
sqlcmd -S .\SQLEXPRESS -E -i sql\01_configuracion_canal.sql
```

(Si tu instancia no es `.\SQLEXPRESS`, cambio el servidor en el comando y en `Web.config`.)

### 2) Sitio
Abro la solución en Visual Studio y doy **F5** (IIS Express).

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

## Decisiones que tomé (resumen)

1. **Parametrizar por BD**, no por código, para no romper canales en producción al sumar uno nuevo.  
2. **Session** solo para la empresa elegida (y el canal), no para secretos como la API key.  
3. **Handler en el mismo sitio** para cumplir el servicio simulado sin complicar el arranque.  
4. **Mensajes amigables + log** cuando el servicio falla (401, timeout, etc.).

Si algo no levanta, lo primero que reviso es: connection string, que el script SQL se haya ejecutado, y que IIS Express esté corriendo el proyecto correcto.
