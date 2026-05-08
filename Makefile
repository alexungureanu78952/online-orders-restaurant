.PHONY: help docker-build docker-up docker-down docker-logs docker-clean docker-status \
        db-init db-backup db-restore db-shell db-stats \
        app-logs app-shell app-health \
        dev-setup prod-setup

# Color output
BLUE := \033[0;34m
GREEN := \033[0;32m
RED := \033[0;31m
YELLOW := \033[0;33m
NC := \033[0m # No Color

# Default target
.DEFAULT_GOAL := help

help: ## Display this help message
	@echo "$(BLUE)Restaurant Order Management System - Docker Commands$(NC)"
	@echo ""
	@echo "$(GREEN)Setup & Build:$(NC)"
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(BLUE)%-25s$(NC) %s\n", $$1, $$2}'

# ===================================================
# Docker Compose Management
# ===================================================

docker-build: ## Build Docker images (fresh build, no cache)
	@echo "$(BLUE)Building Docker images...$(NC)"
	docker-compose build --no-cache
	@echo "$(GREEN)✓ Build complete$(NC)"

docker-up: ## Start all containers (detached)
	@echo "$(BLUE)Starting containers...$(NC)"
	docker-compose up -d
	@echo "$(YELLOW)Waiting for services to be ready...$(NC)"
	@sleep 5
	docker-compose ps
	@echo "$(GREEN)✓ All services started$(NC)"

docker-down: ## Stop and remove containers (volumes persist)
	@echo "$(BLUE)Stopping containers...$(NC)"
	docker-compose down
	@echo "$(GREEN)✓ Containers stopped$(NC)"

docker-restart: ## Restart all services
	@echo "$(BLUE)Restarting services...$(NC)"
	docker-compose restart
	@sleep 3
	docker-compose ps
	@echo "$(GREEN)✓ Services restarted$(NC)"

docker-logs: ## View live logs from all services
	docker-compose logs -f

docker-status: ## Show container status and resource usage
	@echo "$(BLUE)Container Status:$(NC)"
	docker-compose ps
	@echo ""
	@echo "$(BLUE)Resource Usage:$(NC)"
	docker stats --no-stream

docker-clean: ## Remove containers, images, and volumes (DESTRUCTIVE!)
	@echo "$(RED)WARNING: This will delete all containers, images, and volumes!$(NC)"
	@read -p "Are you sure? (yes/no): " confirm && [ "$$confirm" = "yes" ] || (echo "Cancelled"; exit 1)
	docker-compose down -v
	docker image prune -a
	rm -rf docker-volumes/
	@echo "$(GREEN)✓ Cleanup complete$(NC)"

# ===================================================
# Database Management
# ===================================================

db-init: ## Initialize database (create tables, procedures, indexes)
	@echo "$(BLUE)Initializing database...$(NC)"
	@read -p "Enter SA password: " pwd; \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" -i /scripts/02_CreateTables.sql && \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" -i /scripts/03_CreateStoredProcedures.sql && \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" -i /scripts/04_CreateIndexes.sql && \
	echo "$(GREEN)✓ Database initialized$(NC)" || echo "$(RED)✗ Initialization failed$(NC)"

db-backup: ## Backup database to docker-volumes/backup/
	@echo "$(BLUE)Creating database backup...$(NC)"
	@mkdir -p docker-volumes/backup
	@read -p "Enter SA password: " pwd; \
	DATE=$$(date +%Y%m%d_%H%M%S); \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" \
		-Q "BACKUP DATABASE RestaurantOrderManagement TO DISK = '/var/opt/mssql/backup/backup_$$DATE.bak'" && \
	echo "$(GREEN)✓ Backup created: backup_$$DATE.bak$(NC)" || echo "$(RED)✗ Backup failed$(NC)"

db-restore: ## Restore database from backup
	@echo "$(BLUE)Available backups:$(NC)"
	@ls -lh docker-volumes/backup/*.bak 2>/dev/null || echo "No backups found"
	@echo ""
	@read -p "Enter backup filename (e.g., backup_20240508_120000.bak): " backupfile; \
	read -p "Enter SA password: " pwd; \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" \
		-Q "RESTORE DATABASE RestaurantOrderManagement FROM DISK = '/var/opt/mssql/backup/$$backupfile'" && \
	echo "$(GREEN)✓ Database restored$(NC)" || echo "$(RED)✗ Restore failed$(NC)"

db-shell: ## Open SQL command-line in database container
	@echo "$(BLUE)Connecting to database...$(NC)"
	@read -p "Enter SA password: " pwd; \
	docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" -d RestaurantOrderManagement

db-stats: ## Show database statistics
	@echo "$(BLUE)Database Statistics:$(NC)"
	@read -p "Enter SA password: " pwd; \
	docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" \
		-Q "SELECT name, size * 8.0 / 1024 as SizeMB FROM sys.master_files WHERE name = 'RestaurantOrderManagement_Primary'" && \
	docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" \
		-Q "SELECT COUNT(*) as TableCount FROM information_schema.tables WHERE table_schema = 'dbo'" && \
	docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" \
		-Q "SELECT COUNT(*) as ProcedureCount FROM sys.procedures WHERE schema_id = 1"

# ===================================================
# Application Management
# ===================================================

app-logs: ## View application service logs
	docker-compose logs -f app-services

app-shell: ## Get shell access to app container
	docker exec -it restaurant-app-services /bin/sh

app-health: ## Check application health
	@echo "$(BLUE)Application Health Check:$(NC)"
	@docker-compose ps app-services
	@echo ""
	@docker exec restaurant-app-services echo "$(GREEN)✓ Container responsive$(NC)" || echo "$(RED)✗ Container not responsive$(NC)"

# ===================================================
# Development Setup
# ===================================================

dev-setup: ## Setup development environment
	@echo "$(BLUE)Setting up development environment...$(NC)"
	cp .env.example .env
	@echo "$(YELLOW)Note: Edit .env with your settings$(NC)"
	docker-compose up -d
	@echo "$(GREEN)✓ Development environment ready$(NC)"
	@echo ""
	@echo "Next steps:"
	@echo "  1. Edit .env with your database password"
	@echo "  2. Run 'make db-init' to initialize database"
	@echo "  3. Connect your WPF app to localhost,1433"

prod-setup: ## Setup production environment
	@echo "$(RED)WARNING: Production setup!$(NC)"
	@read -p "Continue? (yes/no): " confirm && [ "$$confirm" = "yes" ] || (echo "Cancelled"; exit 1)
	cp .env.example .env
	@echo "$(YELLOW)Edit .env with STRONG credentials before starting!$(NC)"
	@echo ""
	@echo "Configuration needed in .env:"
	@echo "  - DATABASE_PASSWORD=<use 'openssl rand -base64 32'>"
	@echo "  - DATA_PATH=/secure/backup/path"
	@echo ""
	@echo "Then run: docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d"

# ===================================================
# Utility Commands
# ===================================================

version: ## Show Docker and Docker Compose versions
	@echo "$(BLUE)Docker Version:$(NC)"
	@docker --version
	@echo "$(BLUE)Docker Compose Version:$(NC)"
	@docker-compose --version
	@echo "$(BLUE).NET Version (in container):$(NC)"
	@docker exec restaurant-app-services dotnet --version

env-check: ## Verify .env configuration
	@echo "$(BLUE)Environment Configuration:$(NC)"
	@if [ -f .env ]; then \
		grep -v "^#" .env | grep -v "^$$" | sed 's/=.*/=***HIDDEN***/'; \
	else \
		echo "$(RED).env file not found. Run 'make dev-setup'$(NC)"; \
	fi

disk-usage: ## Show Docker disk usage
	@echo "$(BLUE)Docker Disk Usage:$(NC)"
	docker system df
	@echo ""
	@echo "$(BLUE)Volume Sizes:$(NC)"
	@du -sh docker-volumes/* 2>/dev/null || echo "No volumes found"

test-connection: ## Test database connection from host
	@echo "$(BLUE)Testing database connection...$(NC)"
	@read -p "Enter SA password: " pwd; \
	docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$$pwd" -Q "SELECT @@VERSION" | head -3 && \
	echo "$(GREEN)✓ Connection successful$(NC)" || echo "$(RED)✗ Connection failed$(NC)"

# ===================================================
# Info Commands
# ===================================================

info: ## Show project information
	@echo "$(BLUE)Restaurant Order Management System$(NC)"
	@echo "$(BLUE)Docker Setup Information$(NC)"
	@echo ""
	@echo "Services:"
	@echo "  - sqlserver:        SQL Server 2019"
	@echo "  - app-services:     .NET 6.0 Application"
	@echo ""
	@echo "Ports:"
	@echo "  - 1433:    SQL Server"
	@echo "  - 5000:    HTTP API"
	@echo "  - 5001:    HTTPS API"
	@echo ""
	@echo "Data Location:"
	@echo "  - docker-volumes/data        SQL Server data"
	@echo "  - docker-volumes/logs        SQL Server logs"
	@echo "  - docker-volumes/backup      Database backups"
	@echo "  - docker-volumes/app-logs    Application logs"
	@echo ""
	@echo "Documentation:"
	@echo "  - DOCKER.md       Complete Docker guide"
	@echo "  - .env.example    Environment template"
	@echo "  - docker-compose.yml    Service orchestration"

# ===================================================
# Quick Commands
# ===================================================

.PHONY: start stop restart reset logs
start: docker-up ## Alias for docker-up
stop: docker-down ## Alias for docker-down
restart: docker-restart ## Alias for docker-restart
logs: docker-logs ## Alias for docker-logs
reset: docker-clean ## Alias for docker-clean (DESTRUCTIVE!)
