# Maze of HATEOAS - Makefile
# Run `make help` for available targets

.PHONY: help build build-api build-solver build-ui test run run-api run-ui run-solver run-all run-ui-dev clean logs

# Default target
.DEFAULT_GOAL := help

# Colors for help output
CYAN := \033[36m
RESET := \033[0m

##@ Build

build: build-api build-solver build-ui ## Build all Docker images
	@echo "All images built successfully"

build-api: ## Build API Docker image
	docker build -t maze-api -f Dockerfile .

build-solver: ## Build solver Docker image
	docker build -t maze-solver -f Dockerfile.solver .

build-ui: ## Build UI Docker image
	docker build -t maze-dashboard -f maze-dashboard/Dockerfile maze-dashboard/

##@ Testing

test: ## Run all tests in container
	docker compose -f docker-compose.test.yml up --build --abort-on-container-exit
	@echo "Tests complete. Results in ./TestResults/"

test-filter: ## Run filtered tests (usage: make test-filter FILTER="ClassName")
	docker compose -f docker-compose.test.yml run --rm test dotnet test --filter "FullyQualifiedName~$(FILTER)" --verbosity normal

##@ Running Services

run: run-all ## Run API, UI, and solver (alias for run-all)

run-api: ## Run API only (port 8080)
	docker compose up --build api

run-ui: ## Run API and UI only (ports 8080, 5173)
	docker compose up --build

run-solver: ## Run API and solver together
	docker compose -f docker-compose.solver.yml up --build

run-all: ## Run API, UI, and solver in Docker (ports 8080, 5173)
	docker compose -f docker-compose.yml -f docker-compose.solver.yml up --build

run-ui-dev: ## Run UI in development mode (requires API running)
	@echo "Starting UI dev server... (API must be running on port 8080)"
	cd maze-dashboard && npm install && npm run dev

run-dev: ## Run API in Docker + UI in dev mode (hot reload)
	@echo "Starting API in background..."
	docker compose up --build -d api
	@echo "Starting UI dev server..."
	cd maze-dashboard && npm install && npm run dev

##@ Cleanup

clean: ## Stop and remove all containers
	docker compose down --remove-orphans
	docker compose -f docker-compose.solver.yml down --remove-orphans
	docker compose -f docker-compose.test.yml down --remove-orphans

clean-all: clean ## Stop containers and remove images
	docker rmi maze-api maze-solver maze-dashboard 2>/dev/null || true

##@ Utilities

logs: ## View logs from running containers
	docker compose logs -f

logs-api: ## View API logs only
	docker compose logs -f api

logs-solver: ## View solver logs
	docker compose -f docker-compose.solver.yml logs -f solver

status: ## Show running containers
	docker compose ps
	@echo ""
	docker compose -f docker-compose.solver.yml ps

##@ Help

help: ## Show this help message
	@echo "Maze of HATEOAS - Available targets:"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf ""} /^[a-zA-Z_-]+:.*?##/ { printf "  $(CYAN)%-15s$(RESET) %s\n", $$1, $$2 } /^##@/ { printf "\n%s\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
	@echo ""
	@echo "Examples:"
	@echo "  make test                    # Run all tests"
	@echo "  make run                     # Run API + UI + solver"
	@echo "  make run-solver              # Run API + solver"
	@echo "  make run-dev                 # Run API (Docker) + UI (hot reload)"
	@echo "  make test-filter FILTER=Cell # Run tests matching 'Cell'"
