# Docker & Containerization Guide

**Restaurant Order Management System - Containerization (Phase 12)**

**Date**: May 8, 2026  
**Docker Compose Version**: 3.8  
**Base Images**: 
- SQL Server 2019: `mcr.microsoft.com/mssql/server:2019-latest`
- .NET Runtime: `mcr.microsoft.com/dotnet/runtime:6.0-alpine`
- .NET SDK: `mcr.microsoft.com/dotnet/sdk:6.0-alpine` (build stage)

---

## Quick Start

### 1. Prerequisites

- Docker Desktop (20.10+) or Docker Engine (20.10+) with Docker Compose (2.0+)
- At least 4GB RAM allocated to Docker
- 5GB free disk space for volumes

### 2. Clone Configuration

```bash
# Copy environment template
cp .env.example .env

# Edit .env with your preferred values
# At minimum, change:
#   DATABASE_PASSWORD=<your-strong-password>
```

### 3. Start the Application

```bash
# Build and start all services
docker-compose up -d

# View startup logs
docker-compose logs -f

# Check service health
docker-compose ps
```

**Expected Output:**
```
NAME                           STATUS                 PORTS
restaurant-db                  Up (healthy)           0.0.0.0:1433->1433/tcp
restaurant-app-services       Up                      0.0.0.0:5000->5000/tcp
```

### 4. Connect to Database

**From Host Machine:**
```
Server: localhost,1433
User: sa
Password: <your-database-password>
Database: RestaurantOrderManagement
```

**From Desktop Application:**
```
Server: localhost,1433
User: sa
Password: <your-database-password>
Database: RestaurantOrderManagement
```

**From Container Network:**
```
Server: sqlserver,1433
User: sa
Password: <your-database-password>
Database: RestaurantOrderManagement
```

---

## Architecture

### Services

#### 1. **sqlserver** (SQL Server 2019)
- **Image**: `mcr.microsoft.com/mssql/server:2019-latest`
- **Port**: 1433 (configurable via `DATABASE_PORT`)
- **Volumes**:
  - Data: `/var/opt/mssql/data` → `docker-volumes/data`
  - Logs: `/var/opt/mssql/log` → `docker-volumes/logs`
  - Backups: `/var/opt/mssql/backup` → `docker-volumes/backup`
- **Health Check**: SQL Server connectivity test every 10s
- **Startup Time**: 40-60 seconds
- **Resource Limits**: 2GB memory (default, configurable)

**Features:**
- Developer Edition (free, unlimited features)
- Enterprise-class features enabled
- SA (system administrator) account
- All database scripts available in `/scripts/`
- Automatic restart on failure

#### 2. **app-services** (.NET 6.0 Services Layer)
- **Image**: Built from `Dockerfile` (multi-stage build)
- **Base**: Alpine Linux + .NET 6.0 runtime (lightweight)
- **Ports**: 5000 (HTTP), 5001 (HTTPS)
- **Depends On**: `sqlserver` service (waits for health check)
- **Volumes**:
  - Logs: `/app/logs` → `docker-volumes/app-logs`
  - Config: `/app/config/` (read-only)
  - Database Scripts: Available at `/app/Database/`
- **Startup Time**: 10-20 seconds (after DB ready)
- **Resource Limits**: 1GB memory (default, configurable)

**Features:**
- Multi-stage build for minimal image size (~200MB)
- Automatic database readiness check
- Graceful shutdown support
- Comprehensive logging
- Health monitoring

---

## Volume Structure

```
docker-volumes/
├── data/                 # SQL Server data files (.mdf, .ldf)
│   └── RestaurantOrderManagement_Primary.mdf
├── logs/                 # SQL Server error logs
│   └── errorlog
├── backup/               # Database backups (.bak files)
│   └── RestaurantOrderManagement_backup.bak
└── app-logs/            # Application logs
    └── app-*.log
```

**Persistence**: All volumes persist across container restarts and upgrades. To reset:
```bash
# WARNING: Destructive!
rm -rf docker-volumes/
docker-compose down -v
docker-compose up -d
```

---

## Configuration

### Environment Variables (.env)

| Variable | Default | Description |
|----------|---------|-------------|
| `DATABASE_SERVER` | `sqlserver` | SQL Server hostname (use service name) |
| `DATABASE_PORT` | `1433` | SQL Server port |
| `DATABASE_NAME` | `RestaurantOrderManagement` | Database name |
| `DATABASE_USERNAME` | `sa` | SQL Server login user |
| `DATABASE_PASSWORD` | `YourStrongPassword123!` | **CHANGE THIS** to strong password |
| `ASPNETCORE_ENVIRONMENT` | `Production` | App environment (Production/Staging/Development) |
| `DATA_PATH` | `./docker-volumes/` | Base path for persistent volumes |

### Docker Compose Overrides

For development with different settings:

```bash
# Start with Staging environment
ASPNETCORE_ENVIRONMENT=Staging docker-compose up -d

# Use custom data path
DATA_PATH=/data/restaurant docker-compose up -d

# Custom database password
DATABASE_PASSWORD=MySecurePass123! docker-compose up -d
```

---

## Management Commands

### Container Lifecycle

```bash
# View all services and their status
docker-compose ps

# Start stopped services
docker-compose start

# Stop running services (data persists)
docker-compose stop

# Restart services
docker-compose restart

# Stop and remove containers (volumes persist)
docker-compose down

# Stop, remove containers, AND remove volumes (destructive!)
docker-compose down -v

# Remove dangling images
docker image prune
```

### Logs & Monitoring

```bash
# View live logs from all services
docker-compose logs -f

# View only database logs
docker-compose logs -f sqlserver

# View only app logs
docker-compose logs -f app-services

# Tail last 100 lines
docker-compose logs -f --tail=100

# Follow logs with timestamps
docker-compose logs -f --timestamps

# Export logs to file
docker-compose logs > combined-logs.txt
```

### Database Management

```bash
# Execute SQL command in container
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD"

# Connect interactively (if sqlcmd available on host)
sqlcmd -S localhost,1433 -U sa -P "PASSWORD" -d RestaurantOrderManagement

# Check database size and status
docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -Q "SELECT name, size FROM sys.master_files WHERE name = 'RestaurantOrderManagement_Primary'"

# Backup database
docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -Q "BACKUP DATABASE RestaurantOrderManagement TO DISK = '/var/opt/mssql/backup/backup.bak'"
```

### Performance & Resource Monitoring

```bash
# Show container stats (CPU, memory, network)
docker stats

# Show detailed container info
docker inspect restaurant-db
docker inspect restaurant-app-services

# Check health status
docker-compose ps
```

---

## Networking

### Internal Network
- **Name**: `restaurant-network` (bridge driver)
- **Subnet**: 172.28.0.0/16
- **Services** communicate by hostname:
  - `sqlserver:1433` (from app container)
  - `app-services:5000` (from external)

### Port Mapping
```
Host          Container Service
-----         -------- -------
localhost:1433 → 1433   (SQL Server)
localhost:5000 → 5000   (HTTP)
localhost:5001 → 5001   (HTTPS)
```

**Access Examples:**
```csharp
// From WPF app (running on host)
string connectionString = "Server=localhost,1433;Database=RestaurantOrderManagement;User Id=sa;Password=...";

// From another container
string connectionString = "Server=sqlserver,1433;Database=RestaurantOrderManagement;User Id=sa;Password=...";
```

---

## Database Initialization

### Manual Initialization (if needed)

```bash
# Connect to database
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD"

# Execute scripts in order
1> :r /scripts/02_CreateTables.sql
2> :r /scripts/03_CreateStoredProcedures.sql
3> :r /scripts/04_CreateIndexes.sql
4> GO
```

**Or using automated script:**
```bash
# Create init script
cat > init-database.sh << 'EOF'
#!/bin/bash
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$1" -i /scripts/02_CreateTables.sql
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$1" -i /scripts/03_CreateStoredProcedures.sql
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$1" -i /scripts/04_CreateIndexes.sql
EOF

chmod +x init-database.sh
./init-database.sh "YourPassword123!"
```

---

## Security Best Practices

### 1. **Change Default Credentials**

❌ **NEVER** use default passwords in production:
```yaml
# Bad - exposes password
DATABASE_PASSWORD=YourStrongPassword123!
```

✅ **DO** use strong, unique passwords:
```bash
# Generate strong password
openssl rand -base64 32

# Use in .env (not in docker-compose.yml)
DATABASE_PASSWORD=Tr0pic@lM@ng0es_2024!Secure42
```

### 2. **Use .env File (Not Committed)**

```bash
# Create .env (git-ignored)
echo ".env" >> .gitignore
cp .env.example .env
# Edit .env with your credentials
```

### 3. **Limit Container Ports**

For production, bind only to localhost:
```yaml
# docker-compose-prod.yml
services:
  sqlserver:
    ports:
      - "127.0.0.1:1433:1433"  # Only localhost
  app-services:
    ports:
      - "127.0.0.1:5000:5000"  # Only localhost
```

### 4. **Use Non-Root User** (Advanced)

```dockerfile
# In Dockerfile, after services installed
RUN useradd -m -u 1000 appuser
USER appuser
```

### 5. **Regular Backups**

```bash
# Automated daily backup script
cat > backup-database.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec restaurant-db /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$1" \
  -Q "BACKUP DATABASE RestaurantOrderManagement TO DISK = '/var/opt/mssql/backup/backup_$DATE.bak'"
echo "✓ Backup created: backup_$DATE.bak"
EOF

chmod +x backup-database.sh
# Add to crontab for daily runs: 0 2 * * * /path/to/backup-database.sh "PASSWORD"
```

---

## Dockerfile Explanation (Multi-Stage Build)

### Stage 1: Build
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
# - Uses full SDK for compilation
# - Not included in final image (smaller)
# - Compiles all projects to Release configuration
```

### Stage 2: Runtime
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
# - Uses minimal runtime-only image
# - ~200MB final image (vs 1GB+ with SDK)
# - Only runtime libraries needed
# - Faster deployments
```

**Benefits of Multi-Stage:**
- **Size**: 80% reduction (1GB SDK → 200MB runtime)
- **Security**: No SDK/build tools in production
- **Speed**: Faster image pulls and container starts
- **Layering**: Efficient caching of build layers

---

## Troubleshooting

### Database Won't Start

```bash
# Check logs
docker-compose logs sqlserver

# Common issue: Port already in use
lsof -i :1433

# Solution: Change port in .env
DATABASE_PORT=1434
docker-compose down
docker-compose up -d
```

### App Can't Connect to Database

```bash
# Check network connectivity
docker exec app-services ping sqlserver

# Check database is ready
docker exec -it restaurant-db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -Q "SELECT @@VERSION"

# View app startup logs
docker-compose logs app-services
```

### Out of Disk Space

```bash
# Check Docker disk usage
docker system df

# Clean up unused images/volumes
docker system prune -a --volumes
```

### Performance Issues

```bash
# Check container resource usage
docker stats

# Increase memory allocation (Docker Desktop settings)
# Settings → Resources → Memory: Increase to 4GB+

# Check SQL Server logs
docker exec restaurant-db tail -f /var/opt/mssql/log/errorlog
```

---

## Production Deployment

### Deployment Checklist

- [ ] Use `.env` file with strong credentials (not in VCS)
- [ ] Run on `Production` environment
- [ ] Set up automated backups (daily recommended)
- [ ] Monitor disk space for volumes
- [ ] Configure Docker restart policy: `unless-stopped`
- [ ] Set resource limits to prevent host resource exhaustion
- [ ] Use SSL certificates for HTTPS
- [ ] Implement log rotation (already configured in docker-compose.yml)
- [ ] Regular security updates: `docker pull [image]` and redeploy
- [ ] Test restore procedure from backup

### Production docker-compose.yml Override

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  sqlserver:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
    ports:
      - "127.0.0.1:1433:1433"  # Localhost only

  app-services:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1G
        reservations:
          cpus: '1'
          memory: 512M
    ports:
      - "127.0.0.1:5000:5000"
```

**Run Production:**
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## Development Tips

### Hot Reload During Development

For development with code changes:

```bash
# Use development environment
ASPNETCORE_ENVIRONMENT=Development docker-compose up

# Mount source code as volume (in override file)
app-services:
  volumes:
    - ./RestaurantOrderManagement:/src
    - /src/bin
    - /src/obj
```

### Debugging Containers

```bash
# Get shell access to container
docker exec -it restaurant-app-services /bin/sh

# List running processes
docker exec restaurant-app-services ps aux

# Check environment variables
docker exec restaurant-app-services env
```

### Database Development Workflow

```bash
# Start fresh database
docker-compose down -v
docker-compose up -d sqlserver

# Wait for readiness
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "PASSWORD" -Q "SELECT 1"

# Apply migrations/scripts
./init-database.sh "PASSWORD"

# Develop and test
# ...

# Cleanup
docker-compose down
```

---

## Related Documentation

- [Docker Official Documentation](https://docs.docker.com/)
- [Docker Compose Specification](https://github.com/compose-spec/compose-spec/blob/master/spec.md)
- [Microsoft SQL Server on Linux/Docker](https://hub.docker.com/_/microsoft-mssql-server)
- [.NET in Docker](https://github.com/dotnet/dotnet-docker)
- [Restaurant Order Management - Main README](../README.md)

---

## Summary

**Benefits of This Docker Setup:**
- ✅ Consistent environment across dev/test/prod
- ✅ Zero installation for database (Docker handles it)
- ✅ Data persistence across restarts
- ✅ Easy backups and recovery
- ✅ Scalable architecture
- ✅ Security best practices (parameterized queries, credential management)
- ✅ Production-ready configuration
- ✅ Comprehensive monitoring and logging

**Estimated Resource Usage:**
- **Memory**: 2-3GB (SQL Server 2GB + App 1GB)
- **Disk**: 5GB initial + database growth
- **CPU**: 1-2 cores under normal load

**Next Steps:**
1. Copy `.env.example` to `.env` and update credentials
2. Run `docker-compose up -d`
3. Wait for database health check to pass
4. Initialize database from WPF app or manual scripts
5. Start developing/deploying!
