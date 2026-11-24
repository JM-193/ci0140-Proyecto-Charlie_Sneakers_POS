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
