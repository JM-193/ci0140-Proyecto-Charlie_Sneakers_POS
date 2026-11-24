# Charlie's Sneakers POS

## Base de datos y pruebas con Make

El backend utiliza **SQL Server LocalDB** y **Entity Framework Core**. Las
migraciones se encuentran en `backend/CharliesSneakersPOS.Infrastructure/Persistence/Migrations`.

### Requisitos (Windows)

- .NET SDK 10, GNU Make y una terminal compatible con `sh` (por ejemplo, Git Bash).
- SQL Server Express **LocalDB** instalado (`sqllocaldb` accesible en `PATH`).
- Las herramientas de EF Core se restauran mediante `dotnet tool restore`.

Desde la raíz del repositorio:

```bash
make help              # Mostrar todos los objetivos disponibles
make db-start          # Iniciar la instancia (localdb)\MSSQLLocalDB
make db-status         # Consultar el estado de LocalDB
make db-migrate        # Crear/actualizar la BD aislada CharliesSneakersPOS_MakeDev
make run-db            # Aplicar migraciones y ejecutar la API con esa BD
make db-test           # Ejecutar pruebas de integración reales con SQL Server
make test-all          # Pruebas habituales + integración
```

Puedes cambiar el nombre de la base aislada:

```bash
make db-migrate DB_NAME=CharliesSneakersPOS_MiEntorno
make run-db DB_NAME=CharliesSneakersPOS_MiEntorno
```

**Importante:** `make db-migrate` opera sobre una base de desarrollo separada
por defecto (`CharliesSneakersPOS_MakeDev`) y **no borra datos**. El objetivo
`make run` original conserva su configuración de `appsettings.Development.json`
(`CharliesSneakersPOS`) y no se modifica. Para apuntar a la base preparada
por el Makefile utiliza `make run-db`.

**Pruebas de integración:** `make db-test` inicia la instancia
`MSSQLLocalDB` y ejecuta el proyecto
`backend/CharliesSneakersPOS.IntegrationTests`. Cada prueba crea su propia base
temporal, aplica migraciones y la elimina al terminar; no usa la base
`CharliesSneakersPOS_MakeDev` ni la base habitual de desarrollo. **No necesitas
ejecutar `make db-migrate` antes de `make db-test`.**

La base creada con las migraciones contiene el esquema, pero no datos de
productos de ejemplo. Las pruebas de integración generan sus propios registros.
SQL Server LocalDB funciona solo en Windows: para Linux/macOS o CI se necesita
otro proveedor/instancia y adaptar las pruebas que actualmente tienen
`(localdb)\MSSQLLocalDB` fijo.

### Datos de demostración

`make db-seed` inserta 9 productos de ejemplo (varias tallas y colores, con
código de barras y uno con stock 0) en la base aislada `DB_NAME`. Es
idempotente: solo inserta los SKU que todavía no existen. Requiere `sqlcmd`
(`winget install Microsoft.Sqlcmd`) y haber corrido antes `make db-migrate`.

### Linux / WSL y CI

LocalDB solo existe en Windows. En Linux o WSL se puede usar SQL Server en
Docker y sobrescribir la cadena de conexión con una variable de entorno, sin
modificar `appsettings*.json`:

```bash
export ConnectionStrings__Default='Server=localhost,1433;Database=CharliesSneakersPOS_MakeDev;User ID=sa;Password=<su-contraseña>;TrustServerCertificate=True'
dotnet run --project backend/CharliesSneakersPOS.Api
```

Las pruebas de integración leen el servidor de `TEST_SQL_CONNECTION` (cadena
completa sin base de datos) o `TEST_SQL_SERVER` (solo el servidor, con
autenticación de Windows); si ninguna está definida usan LocalDB. En GitHub
Actions el job **Backend integration (SQL Server)** levanta SQL Server 2022 en
Docker y necesita el secret `MSSQL_SA_PASSWORD` (Settings > Secrets and
variables > Actions).

## Interfaz (frontend)

Interfaz de caja en Vue 3 + TypeScript (carpeta `frontend/`) para **Registrar
venta** (HU-01, 02, 03, 05) y **Abrir / cerrar y cuadrar caja** (HU-09, 10).
El navegador solo habla con Vite; Vite reenvía `/api` a la API en
`http://localhost:5274`, por lo que no se necesita CORS. Los totales, el
pendiente y el vuelto siempre los calcula el backend (`POST /api/sales/preview`).

### Requisitos

- Node.js `^20.19` o `>=22.12` (incluye `npm`).
- La API corriendo con `make run-db` (no `make run`).

### Puesta en marcha

Primera vez:

```bash
make front-install     # npm ci --ignore-scripts
make db-migrate        # esquema en CharliesSneakersPOS_MakeDev
make db-seed           # productos de demostración
```

Luego, en dos terminales:

```bash
make run-db            # Terminal 1: API en http://localhost:5274
make front-dev         # Terminal 2: interfaz en http://localhost:5173
```

Abrir **http://localhost:5173**. Si la caja está cerrada, la aplicación lleva a
"Abrir caja"; después se puede vender en `/venta` y cerrar en `/caja/cerrar`.

Para apuntar a otra API, copiar `frontend/.env.example` a `frontend/.env.local`
y definir `VITE_API_BASE_URL` (vacío = mismo origen a través del proxy).

## Cómo probar la aplicación (paso a paso)

Guía para levantar el backend y la interfaz en Windows y probarlos a mano.
Todos los comandos se ejecutan en **Git Bash** desde la raíz del repositorio.

### 0. Verificar requisitos

```bash
dotnet --version        # 10.x
node -v                 # v20.19+ o v22.12+
make --version          # GNU Make
sqllocaldb info         # debe listar MSSQLLocalDB
sqlcmd -?               # si falta: winget install Microsoft.Sqlcmd (y abrir otra terminal)
```

### 1. Preparar (solo la primera vez)

```bash
make restore            # paquetes NuGet y dotnet-ef
make front-install      # dependencias del frontend
make db-migrate         # crea las tablas en CharliesSneakersPOS_MakeDev
make db-seed            # inserta los productos de demostración
```

`make db-seed` puede repetirse sin duplicar datos. La base habitual
`CharliesSneakersPOS` (la de `make run`) no se toca.

### 2. Levantar backend e interfaz

Se necesitan **dos terminales** abiertas al mismo tiempo:

```bash
# Terminal 1 - API (esperar el mensaje "Now listening on: http://localhost:5274")
make run-db

# Terminal 2 - Interfaz (esperar "Local: http://localhost:5173/")
make front-dev
```

Abrir **http://localhost:5173** en el navegador (Edge, Chrome o Firefox).
Para detener todo: `Ctrl + C` en cada terminal.

### 3. Productos de demostración

El campo de búsqueda funciona como el escáner: escribir (o pegar) el código y
presionar **Enter** agrega el producto directamente. También se puede buscar
por nombre (por ejemplo `Jordan`) y tocar el resultado.

| Código de barras | SKU | Producto | Talla | Precio | Stock |
|---|---|---|---|---|---|
| 7441000000011 | PV-AJ1-CHI-42 | Air Jordan 1 Retro High OG (Chicago) | 42 | ₡95 000 | 5 |
| 7441000000028 | PV-AJ1-CHI-43 | Air Jordan 1 Retro High OG (Chicago) | 43 | ₡95 000 | 3 |
| 7441000000035 | PV-AJ1-BRD-43 | Air Jordan 1 Retro High OG (Bred) | 43 | ₡98 000 | **0** |
| 7441000000042 | PV-AF1-WHT-40 | Nike Air Force 1 '07 (Blanco) | 40 | ₡65 000 | 8 |
| 7441000000059 | PV-AF1-WHT-41 | Nike Air Force 1 '07 (Blanco) | 41 | ₡65 000 | 6 |
| 7441000000066 | PV-AF1-BLK-42 | Nike Air Force 1 '07 (Negro) | 42 | ₡65 000 | 4 |
| 7441000000073 | PV-DNK-PND-39 | Nike Dunk Low Panda | 39 | ₡72 000 | 2 |
| 7441000000080 | PV-SMB-GRN-41 | Adidas Samba OG | 41 | ₡58 000 | 7 |
| 7441000000097 | PV-NB550-GRY-44 | New Balance 550 | 44 | ₡69 000 | 1 |

El stock es el inicial; cada venta registrada lo descuenta.

### 4. Recorrido de prueba sugerido

| # | Acción | Resultado esperado |
|---|---|---|
| 1 | Abrir http://localhost:5173/venta con la caja cerrada | Redirige a "Abrir caja" con el aviso "Debe abrir la caja antes de vender." |
| 2 | Monto inicial `50000` → **Abrir caja** | Pasa a "Registrar venta"; el encabezado muestra **Caja abierta** |
| 3 | Escanear `7441000000042` + Enter | Se agrega Nike Air Force 1: subtotal ₡65 000, IVA ₡8 450, total ₡73 450 |
| 4 | Escanear `7441000000035` + Enter | Mensaje "Este producto no tiene unidades disponibles…" (stock 0) |
| 5 | Escanear `9999` + Enter | "No se encontró el producto. Revise el código o busque por nombre." |
| 6 | Escanear `7441000000097` y tocar **+** | "No hay suficientes unidades. Disponibles: 1…"; la cantidad queda en 1 |
| 7 | Tocar **Quitar** en New Balance 550 | Vuelve a total ₡73 450 |
| 8 | **Cobrar** → **Efectivo** → **₡50 000** → **₡50 000** | Pendiente ₡0 y vuelto ₡26 550 |
| 9 | **Confirmar venta** | "Venta registrada… Vuelto: ₡26 550" y el carrito queda vacío |
| 10 | Venta rápida: escanear `7441000000080` → **Cobrar** → **Tarjeta** → **Monto exacto** → **Confirmar venta** | Se registra en 4 toques (M2.1) |
| 11 | Ir a **Abrir caja** con la caja abierta | Indica que la caja ya está abierta (no permite otra) |
| 12 | **Cerrar caja** → contado `123450` → **Cerrar caja** → **Confirmar cierre** | Esperado ₡123 450 (50 000 + 73 450 en efectivo; la tarjeta no cuenta) → **Cuadrada** |
| 13 | Volver a **Venta** | Redirige a "Abrir caja": tras cerrar no se puede vender |

Para ver **Sobrante** o **Faltante**, repetir con un monto contado mayor o
menor que el esperado. Para probar el mensaje de conexión, detener la API
(`Ctrl + C` en la terminal 1) y realizar cualquier acción en la interfaz.

### 5. Probar solo el backend

Con `make run-db` en ejecución:

- **Archivo `.http`:** abrir
  `backend/CharliesSneakersPOS.Api/CharliesSneakersPOS.Api.http` en Visual
  Studio o en VS Code (extensión REST Client) y ejecutar cada solicitud.
  Reemplazar `@cashSessionId` y `@productVariantId` con los ids reales.
- **OpenAPI:** http://localhost:5274/openapi/v1.json (solo en Development).
- **curl** (Git Bash):

```bash
API=http://localhost:5274/api
curl -s "$API/cash-sessions/current"                         # 404 si no hay caja abierta
curl -s -H 'Content-Type: application/json' -d '{"openingAmount":50000}' "$API/cash-sessions"
curl -s "$API/products?q=7441000000042"                      # buscar por código, SKU o nombre
curl -s -H 'Content-Type: application/json' \
  -d '{"items":[{"productVariantId":"6f1d2c10-0001-4a00-9000-000000000004","quantity":1}],"payments":[{"method":"Cash","amount":80000}]}' \
  "$API/sales/preview"                                       # totales, pendiente y vuelto sin guardar
curl -s -H 'Content-Type: application/json' \
  -d '{"saleId":"<guid-nuevo>","cashSessionId":"<id-caja>","items":[{"productVariantId":"6f1d2c10-0001-4a00-9000-000000000004","quantity":1}],"payments":[{"method":"Cash","amount":80000}]}' \
  "$API/sales"                                               # registrar la venta
curl -s -H 'Content-Type: application/json' -d '{"countedAmount":123450}' \
  "$API/cash-sessions/<id-caja>/close"                       # cerrar y cuadrar
```

Los ids de los productos sembrados son fijos (`6f1d2c10-0001-4a00-9000-00000000000N`,
con N del 1 al 9 en el orden de la tabla). Métodos de pago válidos: `Cash`,
`Card` y `SinpeMobile`.

Pruebas automáticas del backend: `make test` (unitarias), `make db-test`
(integración con LocalDB) y `make coverage` (requiere `make tools` una vez).

### 6. Empezar de cero

Para tener stock completo otra vez sin borrar nada, usar otra base aislada
(el nombre debe repetirse en los tres comandos):

```bash
make db-migrate DB_NAME=CharliesSneakersPOS_Demo2
make db-seed    DB_NAME=CharliesSneakersPOS_Demo2
make run-db     DB_NAME=CharliesSneakersPOS_Demo2
```

### 7. Problemas frecuentes

| Síntoma | Solución |
|---|---|
| La interfaz dice "No se pudo conectar con el servidor…" | La API no está corriendo: ejecutar `make run-db` en otra terminal |
| `make run-db` falla con "address already in use" (puerto 5274) | Ya hay otra API abierta; cerrarla con `Ctrl + C` o cerrar esa terminal |
| `make db-seed` dice que falta `sqlcmd` | `winget install Microsoft.Sqlcmd` y abrir una terminal nueva |
| `make db-seed` dice "Invalid object name 'dbo.ProductVariants'" | Ejecutar antes `make db-migrate` |
| La búsqueda no encuentra productos | Ejecutar `make db-seed` con el mismo `DB_NAME` que `make run-db` |
| Vitest falla con "failed to find the runner" | Usar `make front-test`, o entrar a la carpeta con `cd /C/...` (unidad en mayúscula) |
| Un producto ya no tiene stock | Ver "Empezar de cero" |

### Pruebas y calidad del frontend

```bash
make front-test        # Vitest (pruebas unitarias y de componentes)
make front-lint        # oxlint + ESLint sin --fix, igual que el CI
make front-build       # type-check (vue-tsc) + build de producción
cd frontend && npm run test:coverage   # cobertura (reporte en frontend/coverage)
```

Prueba E2E opcional con Playwright (no corre en el CI). Necesita la API con
datos sembrados (`make db-seed` + `make run-db`) y usa Microsoft Edge instalado:

```bash
cd frontend && npm run test:e2e
```

Recorre abrir caja → venta estándar de 3 productos escaneados → cerrar caja,
cuenta los toques de la venta y falla si pasan de 5 (M2.1). Para usar Chrome:
`PW_CHANNEL=chrome npm run test:e2e`.

**Venta estándar (M2.1):** después de escanear (Enter en el buscador, no cuenta
como toque): **Cobrar → método de pago → Monto exacto → Confirmar venta = 4
toques**. En efectivo también hay botones de billetes y un campo de monto
recibido.

**Mensajes (M2.4):** todos los mensajes de error para el usuario están en
`frontend/src/constants/messages.ts` (en español, sin códigos técnicos y con una
acción sugerida); `frontend/src/services/errorMessages.ts` los asigna según el
endpoint y el código HTTP. El texto del backend nunca se muestra.
