# Docker Quick Start Guide

**Restaurant Order Management System - Phase 12 Containerization**

Get the entire application stack running with Docker in **3 commands**.

---

## 30-Second Setup

```bash
# 1. Copy environment configuration
cp .env.example .env

# 2. Start all services (SQL Server + App)
docker-compose up -d

# 3. Wait for database to be ready (should see "healthy")
docker-compose ps
```

**Result:**
- SQL Server running on `localhost:1433`
- Application services running on `localhost:5000`
- All data persists in `./docker-volumes/`

---

## Verify Everything Works

```bash
# Check container status
docker-compose ps

# View logs to confirm startup
docker-compose logs -f

# Expected output:
# restaurant-db (healthy)
# restaurant-app-services (running)
```

---

## Connect to Database

**From WPF Desktop App:**
```
Server: localhost,1433
User: sa
Password: YourStrongPassword123! (from .env)
Database: RestaurantOrderManagement
```

**From SQL Management Studio:**
```
Server: localhost,1433
Authentication: SQL Server Authentication
Login: sa
Password: (from .env)
```

**From Container Terminal:**
```bash
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD"
```

---

## Common Commands

### View Logs
```bash
docker-compose logs -f              # All services
docker-compose logs -f sqlserver    # Database only
docker-compose logs -f app-services # App only
```

### Stop/Start
```bash
docker-compose stop                 # Stop (data persists)
docker-compose start                # Start again
docker-compose restart              # Restart all
docker-compose down                 # Stop and remove containers
docker-compose down -v              # Remove everything including volumes (destructive!)
```

### Debugging
```bash
docker-compose exec sqlserver /bin/bash          # Database shell
docker-compose exec app-services /bin/sh         # App shell
docker stats                                      # Resource usage
```

### Using Makefile (Recommended)
```bash
make help              # Show all available commands
make docker-up        # Start services
make docker-logs      # View logs
make db-backup        # Backup database
make db-init          # Initialize database schema
```

---

## File Structure

```
├── Dockerfile                 # Multi-stage build
├── docker-compose.yml         # Services orchestration
├── docker-compose.prod.yml    # Production overrides
├── docker-entrypoint.sh       # Initialization script
├── .dockerignore              # Build context exclusions
├── .env.example               # Environment template
├── DOCKER.md                  # Complete documentation
├── Makefile                   # Convenient commands
│
└── docker-volumes/            # Data persistence (created at runtime)
    ├── data/                  # SQL Server data files
    ├── logs/                  # SQL Server logs
    ├── backup/                # Database backups
    └── app-logs/              # Application logs
```

---

## Initial Database Setup

### Option 1: Automated Setup (Recommended)

```bash
make db-init
# Follow prompts to initialize database tables, stored procedures, and indexes
```

### Option 2: Manual SQL Script

```bash
# Connect and run initialization scripts
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -i /scripts/02_CreateTables.sql
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -i /scripts/03_CreateStoredProcedures.sql
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -i /scripts/04_CreateIndexes.sql
```

### Option 3: From WPF App

The WPF application may auto-initialize the database on first connection (check application code).

---

## Environment Variables (.env)

**Required (must edit before first run):**
```bash
DATABASE_PASSWORD=YourStrongPassword123!
# Use: openssl rand -base64 32
```

**Optional (defaults provided):**
```bash
DATABASE_SERVER=sqlserver
DATABASE_PORT=1433
DATABASE_NAME=RestaurantOrderManagement
DATABASE_USERNAME=sa
ASPNETCORE_ENVIRONMENT=Production
DATA_PATH=./docker-volumes/
```

---

## Troubleshooting

### Database Won't Start
```bash
# Check logs
docker-compose logs sqlserver

# Port conflict? Change in .env
DATABASE_PORT=1434
docker-compose down
docker-compose up -d

# Clear volumes and restart
docker-compose down -v
docker-compose up -d
```

### Can't Connect to Database
```bash
# Verify container is running
docker-compose ps

# Check if database is ready
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -Q "SELECT 1"

# Wait longer (first start can take 60 seconds)
sleep 60
docker-compose ps
```

### Out of Disk Space
```bash
# Check Docker disk usage
docker system df

# Clean up unused images/volumes
docker system prune -a --volumes

# Delete specific volume
rm -rf docker-volumes/
docker-compose down -v
```

---

## Development vs. Production

### Development (Default)
```bash
# Simple setup with full logging
docker-compose up -d
ASPNETCORE_ENVIRONMENT=Development
```

### Production (Security-Hardened)
```bash
# Only accessible from localhost, resource limits, enhanced logging
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
ASPNETCORE_ENVIRONMENT=Production
# Edit .env with STRONG credentials
```

---

## Data Persistence

**All data is stored in `./docker-volumes/`:**

```
docker-volumes/
├── data/          # Database files (.mdf, .ldf) - MOST IMPORTANT
├── logs/          # SQL Server logs
├── backup/        # Backup files (.bak)
└── app-logs/      # Application logs
```

**Important:**
- ✅ Data persists across container restarts
- ⚠️  Do NOT delete `docker-volumes/data/` unless you want to lose data
- 📦 Backup `docker-volumes/` regularly for production

**Backup:**
```bash
# Backup database
make db-backup

# Or manually
tar -czf backup-$(date +%Y%m%d_%H%M%S).tar.gz docker-volumes/
```

---

## Next Steps

1. **Initialize Database:**
   ```bash
   make db-init
   ```

2. **Run WPF Application:**
   - Open Visual Studio
   - Build solution
   - Run WPF project
   - Connect to `localhost,1433`

3. **Monitor Services:**
   ```bash
   docker stats           # Real-time resource usage
   docker-compose logs -f # Live logs
   ```

4. **Read Full Documentation:**
   - See [DOCKER.md](DOCKER.md) for complete guide
   - Command reference: `make help`

---

## Common Tasks

| Task | Command |
|------|---------|
| Start all services | `make docker-up` or `docker-compose up -d` |
| View logs | `make docker-logs` or `docker-compose logs -f` |
| Stop services | `make docker-down` or `docker-compose down` |
| Initialize database | `make db-init` |
| Backup database | `make db-backup` |
| Database shell | `make db-shell` |
| App shell | `make app-shell` |
| Check status | `make docker-status` or `docker-compose ps` |
| Use production config | `docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d` |

---

## What's Next?

✅ **Complete**: Docker containerization with best practices
✅ **Complete**: SQL Server database container
✅ **Complete**: .NET application services
✅ **Complete**: Multi-stage builds for minimal images
✅ **Complete**: Health checks and monitoring
✅ **Complete**: Data persistence and backups
✅ **Complete**: Production configuration and security

🎉 **Your application is now fully containerized and ready for deployment!**

---

## Support

**For issues:**
1. Check [DOCKER.md](DOCKER.md) troubleshooting section
2. View logs: `docker-compose logs -f`
3. Check Docker daemon: `docker ps`
4. Verify .env configuration: `make env-check`

**For detailed information:**
- Read [DOCKER.md](DOCKER.md) - Complete guide (60+ sections)
- Check [docker-compose.yml](docker-compose.yml) - Inline comments
- Review [Makefile](Makefile) - Command documentation

---

## Quick Reference Card

```bash
# Print this cheat sheet
cat << 'EOF'
╔════════════════════════════════════════════════════════════╗
║  Restaurant Order Management - Docker Cheat Sheet         ║
╚════════════════════════════════════════════════════════════╝

Setup:
  cp .env.example .env              Copy config template
  docker-compose up -d              Start all services

Monitoring:
  docker-compose ps                 Service status
  docker-compose logs -f            View logs
  docker stats                       Resource usage

Database:
  make db-init                       Initialize schema
  make db-backup                     Create backup
  make db-shell                      Connect to DB

Application:
  make app-logs                      View app logs
  make app-shell                     App container shell
  make app-health                    Health check

Maintenance:
  docker-compose restart             Restart services
  docker-compose down                Stop and remove
  make docker-clean                  Full cleanup (destructive!)

Help:
  make help                          Show all commands
  cat DOCKER.md                      Full documentation
EOF
```

---

**Happy containerizing! 🐳**
