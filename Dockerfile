# Multi-stage Dockerfile for Restaurant Order Management System
# Phase 12: Containerization using Docker best practices
# .NET 6.0+ runtime with SQL Server integration

# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /src

# Copy solution and project files
COPY ["RestaurantOrderManagement/RestaurantOrderManagement.sln", "."]
COPY ["RestaurantOrderManagement/RestaurantOrderManagement.Data/RestaurantOrderManagement.Data.csproj", "RestaurantOrderManagement.Data/"]
COPY ["RestaurantOrderManagement/RestaurantOrderManagement.Services/RestaurantOrderManagement.Services.csproj", "RestaurantOrderManagement.Services/"]
COPY ["RestaurantOrderManagement/RestaurantOrderManagement.WPF/RestaurantOrderManagement.WPF.csproj", "RestaurantOrderManagement.WPF/"]
COPY ["RestaurantOrderManagement/RestaurantOrderManagement.Tests/RestaurantOrderManagement.Tests.csproj", "RestaurantOrderManagement.Tests/"]

# Restore dependencies
RUN dotnet restore "RestaurantOrderManagement.sln" --disable-parallel

# Copy all source code
COPY RestaurantOrderManagement/ .

# Build the solution
RUN dotnet build "RestaurantOrderManagement.sln" -c Release --no-restore --no-incremental

# Publish Data and Services libraries (for backend services)
RUN dotnet publish "RestaurantOrderManagement.Data/RestaurantOrderManagement.Data.csproj" -c Release -o /app/data --no-build --no-restore
RUN dotnet publish "RestaurantOrderManagement.Services/RestaurantOrderManagement.Services.csproj" -c Release -o /app/services --no-build --no-restore

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine

WORKDIR /app

# Install required packages
RUN apk add --no-cache \
    ca-certificates \
    openssl

# Create app directories
RUN mkdir -p /app/data /app/services /app/config /app/logs

# Copy published libraries from build stage
COPY --from=build /app/data /app/data
COPY --from=build /app/services /app/services

# Copy database scripts
COPY Database/ /app/Database/

# Copy configuration files
COPY appsettings.json /app/config/
COPY RestaurantConfig.xml /app/config/

# Set environment variables for database connection
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DATABASE_SERVER=sqlserver
ENV DATABASE_PORT=1433
ENV DATABASE_NAME=RestaurantOrderManagement
ENV DATABASE_USERNAME=sa
ENV DATABASE_PASSWORD=YourStrongPassword123!

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD echo "Container is running. Check logs at /app/logs for details."

# Labels for documentation
LABEL maintainer="Restaurant Order Management Team"
LABEL version="1.0.0"
LABEL description="Restaurant Order Management System - Data and Services Layer"

# Run entrypoint script
COPY docker-entrypoint.sh /app/
RUN chmod +x /app/docker-entrypoint.sh

ENTRYPOINT ["/app/docker-entrypoint.sh"]
CMD ["echo", "Restaurant Order Management System - Services ready"]
