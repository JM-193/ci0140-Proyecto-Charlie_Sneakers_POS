# Charlie's Sneakers POS - comandos de desarrollo
# Uso: make <objetivo>   (make help muestra la lista)

SOLUTION     := CharliesSneakersPOS.slnx
API_PROJECT  := backend/CharliesSneakersPOS.Api
INFRA_PROJECT:= backend/CharliesSneakersPOS.Infrastructure
RUNSETTINGS  := backend/CharliesSneakersPOS.Tests/coverage.runsettings
CONFIG       ?= Debug
MIN_COVERAGE ?= 90
INTEGRATION_PROJECT := backend/CharliesSneakersPOS.IntegrationTests/CharliesSneakersPOS.IntegrationTests.csproj

# SQL Server LocalDB (Windows): instancia que usan las pruebas de integracion.
# Base aislada para ejecutar la API desde make; no modifica CharliesSneakersPOS.
LOCALDB_INSTANCE := MSSQLLocalDB
DB_NAME ?= CharliesSneakersPOS_MakeDev
DB_CONNECTION = Server=(localdb)\$(LOCALDB_INSTANCE);Database=$(DB_NAME);Trusted_Connection=True;TrustServerCertificate=True

.DEFAULT_GOAL := help

.PHONY: help restore build rebuild run watch test lint format format-check \
        coverage coverage-open tools clean ef-add ef-update ef-remove ef-list \
        db-start db-status db-migrate db-test test-all run-db

help: ## Muestra esta ayuda
	@grep -E '^[a-zA-Z_-]+:.*## ' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*## "}; {printf "  %-16s %s\n", $$1, $$2}'

restore: ## Restaura paquetes NuGet y herramientas locales (dotnet-ef)
	dotnet restore $(SOLUTION)
	dotnet tool restore

build: ## Compila la solucion (incluye analisis Roslyn, CONFIG=Release opcional)
	dotnet build $(SOLUTION) -c $(CONFIG)

rebuild: clean build ## Limpia y compila

run: ## Ejecuta la API en modo Development
	ASPNETCORE_ENVIRONMENT=Development dotnet run --project $(API_PROJECT)

watch: ## Ejecuta la API con recarga automatica
	ASPNETCORE_ENVIRONMENT=Development dotnet watch --project $(API_PROJECT)

# --- Base de datos de pruebas / desarrollo (SQL Server LocalDB en Windows) ---
# Las pruebas de integracion usan bases temporales y ejecutan MigrateAsync()
# por su cuenta. Nunca se eliminan ni modifican bases ajenas desde estos targets.
db-start: ## Inicia la instancia LocalDB MSSQLLocalDB (solo Windows)
	@command -v sqllocaldb >/dev/null 2>&1 || { \
		echo "Error: falta SQL Server Express LocalDB (sqllocaldb). Instalarlo en Windows."; exit 1; }
	@sqllocaldb info "$(LOCALDB_INSTANCE)" >/dev/null 2>&1 || \
		sqllocaldb create "$(LOCALDB_INSTANCE)"
	@sqllocaldb start "$(LOCALDB_INSTANCE)"

db-status: ## Muestra el estado de la instancia SQL Server LocalDB
	@sqllocaldb info "$(LOCALDB_INSTANCE)"

db-migrate: db-start ## Crea/actualiza las tablas de la base aislada DB_NAME
	@echo "Aplicando migraciones a $(DB_NAME) (no a la BD habitual de la API)..."
	dotnet tool restore
	ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
		--project $(INFRA_PROJECT) --startup-project $(API_PROJECT) \
		--connection '$(DB_CONNECTION)'

run-db: db-migrate ## Inicia la API contra la base aislada DB_NAME
	ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__Default='$(DB_CONNECTION)' \
		dotnet run --project $(API_PROJECT)

db-test: db-start ## Ejecuta pruebas de integracion con bases temporales LocalDB
	dotnet test $(INTEGRATION_PROJECT) -c $(CONFIG)

test-all: test db-test ## Ejecuta pruebas unitarias y de integracion (Windows)

test: ## Ejecuta las pruebas unitarias
	dotnet test $(SOLUTION) -c $(CONFIG)

lint: ## Analisis Roslyn (advertencias) y verificacion de formato
	dotnet build $(SOLUTION) -c $(CONFIG) --no-incremental -p:EnforceCodeStyleInBuild=true
	dotnet format $(SOLUTION) --verify-no-changes --severity warn --no-restore

format: ## Aplica el formato de codigo automaticamente
	dotnet format $(SOLUTION)

format-check: ## Verifica el formato sin modificar archivos
	dotnet format $(SOLUTION) --verify-no-changes

tools: ## Instala reportgenerator (necesario para make coverage)
	dotnet tool install -g dotnet-reportgenerator-globaltool || \
		dotnet tool update -g dotnet-reportgenerator-globaltool

coverage: ## Pruebas con cobertura de Domain (falla si baja de MIN_COVERAGE)
	rm -rf TestResults coverage-report
	dotnet test $(SOLUTION) -c $(CONFIG) --settings $(RUNSETTINGS) \
		--collect:"XPlat Code Coverage" --results-directory TestResults
	reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
		-targetdir:coverage-report -reporttypes:"Html;TextSummary"
	@cat coverage-report/Summary.txt
	@LINE=$$(grep -oE 'Line coverage: [0-9.]+' coverage-report/Summary.txt | grep -oE '[0-9.]+$$'); \
	awk -v c="$$LINE" -v m="$(MIN_COVERAGE)" 'BEGIN{exit !(c>=m)}' || \
		{ echo "Cobertura $$LINE% menor que el minimo $(MIN_COVERAGE)%"; exit 1; }

coverage-open: ## Abre el reporte HTML de cobertura
	@start coverage-report/index.html 2>/dev/null || xdg-open coverage-report/index.html 2>/dev/null || open coverage-report/index.html

clean: ## Borra bin/obj y reportes generados
	dotnet clean $(SOLUTION)
	rm -rf TestResults coverage-report analysis

# Migraciones EF Core. Ejemplo: make ef-add NAME=AddSomething
ef-add: ## Crea una migracion (NAME=NombreMigracion)
	@test -n "$(NAME)" || { echo "Uso: make ef-add NAME=NombreMigracion"; exit 1; }
	ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add $(NAME) \
		--project $(INFRA_PROJECT) --startup-project $(API_PROJECT) \
		--output-dir Persistence/Migrations

ef-update: ## Aplica las migraciones pendientes a la base de datos
	ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
		--project $(INFRA_PROJECT) --startup-project $(API_PROJECT)

ef-remove: ## Elimina la ultima migracion (si no esta aplicada)
	ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations remove \
		--project $(INFRA_PROJECT) --startup-project $(API_PROJECT)

ef-list: ## Lista las migraciones
	ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations list \
		--project $(INFRA_PROJECT) --startup-project $(API_PROJECT)
