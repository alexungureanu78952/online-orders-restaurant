# Development Quickstart: Restaurant Order Management System

**Purpose**: Get developers up to speed on environment setup, project structure, and development workflow

## Prerequisites

- Windows 10/11 with .NET SDK 6.0 or later installed
- Visual Studio 2022 (Community, Professional, or Enterprise)
- SQL Server 2019 or later (local instance or Azure SQL)
- SQL Server Management Studio (SSMS) or Azure Data Studio
- Git for version control

## Environment Setup

### 1. Clone Repository

```bash
cd d:\GitHub
git clone <repository-url>
cd online-orders-restaurant
git checkout 001-order-management
```

### 2. SQL Server Database Setup

#### Create Database

```sql
-- In SQL Server Management Studio or Azure Data Studio
CREATE DATABASE RestaurantOrderManagement;
GO

USE RestaurantOrderManagement;
GO
```

#### Run Database Scripts (in order)

Execute scripts from `Database/` folder in this order:

```powershell
cd Database
sqlcmd -S <server> -d RestaurantOrderManagement -i 01_CreateSchema.sql
sqlcmd -S <server> -d RestaurantOrderManagement -i 02_CreateTables.sql
sqlcmd -S <server> -d RestaurantOrderManagement -i 03_CreateStoredProcedures.sql
sqlcmd -S <server> -d RestaurantOrderManagement -i 04_CreateIndexes.sql
sqlcmd -S <server> -d RestaurantOrderManagement -i 05_SeedData.sql
```

Or execute each script manually in SSMS.

#### Verify Database Created

```sql
-- Should return all tables
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo';

-- Should return 10+ procedures
SELECT * FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_SCHEMA = 'dbo' AND ROUTINE_TYPE = 'PROCEDURE';
```

### 3. Project Setup in Visual Studio

#### Open Solution

```powershell
cd RestaurantOrderManagement
Start-Process "RestaurantOrderManagement.sln"
```

#### Configure Connection String

Edit `RestaurantOrderManagement.WPF/App.config`:

```xml
<connectionStrings>
  <add name="RestaurantDb" 
       connectionString="Server=<YOUR_SERVER>;Database=RestaurantOrderManagement;Trusted_Connection=true;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

For SQL Server Authentication:
```xml
<add name="RestaurantDb" 
     connectionString="Server=<YOUR_SERVER>;Database=RestaurantOrderManagement;User Id=sa;Password=<YOUR_PASSWORD>;" 
     providerName="System.Data.SqlClient" />
```

#### Restore NuGet Packages

```powershell
dotnet restore
```

Or in Visual Studio: Tools → NuGet Package Manager → Package Manager Console → `Update-Package -Reinstall`

#### Build Solution

```powershell
dotnet build
```

Or in Visual Studio: Build → Build Solution (Ctrl+Shift+B)

#### Run Application

```powershell
dotnet run --project RestaurantOrderManagement.WPF/RestaurantOrderManagement.WPF.csproj
```

Or in Visual Studio: Debug → Start Debugging (F5)

## Configuration

### Application Configuration File

Create/edit `RestaurantConfig.xml` in application working directory:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RestaurantConfiguration>
  <Pricing>
    <MinOrderForFreeShipping>100</MinOrderForFreeShipping>
    <ShippingFee>15</ShippingFee>
    <LargeOrderDiscountThreshold>200</LargeOrderDiscountThreshold>
    <LargeOrderDiscountPercent>10</LargeOrderDiscountPercent>
    <MenuBundleDiscountPercent>15</MenuBundleDiscountPercent>
  </Pricing>
  <Discounts>
    <FrequentOrderThreshold>5</FrequentOrderThreshold>
    <FrequentOrderTimeWindow>30</FrequentOrderTimeWindow>
    <FrequentOrderDiscountPercent>8</FrequentOrderDiscountPercent>
  </Discounts>
  <Inventory>
    <LowStockThreshold>500</LowStockThreshold>
  </Inventory>
</RestaurantConfiguration>
```

## Project Structure Overview

### Layer Breakdown

**Presentation (WPF)**
- Location: `RestaurantOrderManagement.WPF/`
- Contains: Views (.xaml), ViewModels, Converters
- UI logic only; NO business logic
- Data binding to ViewModels required

**Business Logic (Services)**
- Location: `RestaurantOrderManagement.Services/`
- Contains: Service interfaces and implementations
- All validation and orchestration logic
- No database queries (delegates to Data layer)

**Data Access (EF Core + Repositories)**
- Location: `RestaurantOrderManagement.Data/`
- Contains: DbContext, Entity models, Repositories
- Stored procedures called via `context.FromSqlRaw()`
- No business logic in repositories

**Tests**
- Location: `RestaurantOrderManagement.Tests/`
- Unit tests for services and ViewModels
- Integration tests for repositories
- Use xUnit and Moq

## Development Workflow

### Adding a New Feature

1. **Update Data Model** (if needed)
   - Edit entity classes in `RestaurantOrderManagement.Data/Models/`
   - Add Entity Framework migrations: `Add-Migration FeatureName`
   - Apply migration to database: `Update-Database`

2. **Create/Update Service Interface**
   - Add interface in `Services/Interfaces/`
   - Define contract clearly

3. **Implement Service**
   - Create implementation in `Services/Implementations/`
   - Add business logic, validation
   - Call repository methods or stored procedures

4. **Create ViewModel** (if UI change)
   - Create ViewModel in `WPF/ViewModels/`
   - Inherit from `ObservableObject` (MVVM Toolkit)
   - Implement properties with `[ObservableProperty]`
   - Implement commands with `[RelayCommand]`

5. **Create View** (if UI change)
   - Create XAML in `WPF/Views/`
   - Bind to ViewModel properties
   - NO code-behind logic

6. **Write Tests**
   - Unit tests in `Tests/Services/`
   - Mock repository and external dependencies
   - Test business logic thoroughly

7. **Commit Changes**
   ```bash
   git add .
   git commit -m "Feature: [Description]"
   git push
   ```

### Running Tests

```powershell
# Run all tests
dotnet test

# Run tests in specific project
dotnet test RestaurantOrderManagement.Tests/RestaurantOrderManagement.Tests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

## Generating Reports

The application provides built-in reporting capabilities for business analytics:

### Available Reports

1. **Order Summary** - Order counts and averages by status
2. **Revenue Report** - Revenue breakdown by order status
3. **Inventory Status** - Current stock levels by category
4. **Order Details** - Detailed order information with customer and revenue data

### Accessing Reports

1. Log in as Employee (use employee account credentials)
2. Navigate to Reports tab in main navigation
3. Select date range for analysis (defaults to last 30 days)
4. Click report button to generate:
   - "Order Summary" - Order count and value metrics
   - "Revenue Report" - Revenue analytics with shipping and discounts
   - "Inventory Status" - Stock levels and low-stock alerts
   - "Order Details" - Full order information with customer contact

### Exporting to CSV

After generating a report:

1. Click "Export to CSV" button below the report
2. File is saved to: `C:\Users\{YourUsername}\Documents\RestaurantOrderManagement_Reports\`
3. Filename format: `ReportType_yyyyMMdd_HHmmss.csv`
4. Open in Excel or any spreadsheet application

### Report Data Sources

All reports use parameterized stored procedures for security:

- `sp_GetOrderSummary` - Aggregates orders by status
- `sp_GetRevenueSummary` - Aggregates revenue metrics by status
- `sp_GetInventorySummary` - Gets current inventory levels
- `sp_GetOrdersByDateRange` - Retrieves detailed order information

## Debugging Tips

### Debug Stored Procedures

1. Set breakpoint in SQL Server Management Studio
2. Execute procedure:
   ```sql
   EXEC sp_SearchProducts @keyword = 'soup', @allergen = NULL
   ```
3. Step through in debugger

### Debug Data Binding Issues

- In ViewModel: Verify `[ObservableProperty]` attributes on changed properties
- In XAML: Set binding to throw on error: `<Binding Path="PropertyName" Mode="TwoWay" NotifyOnValidationError="True" />`
- Run app with Debug output window open to see binding errors

### Debug Entity Framework Queries

Add to DbContext configuration:
```csharp
optionsBuilder.LogTo(Console.WriteLine);
```

Or use:
```csharp
var services = new ServiceCollection();
services.AddDbContext<RestaurantDbContext>(options =>
    options.UseSqlServer(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Debug));
```

## Common Issues

### Issue: "Server does not exist" when connecting to SQL Server

**Solution**:
- Verify SQL Server service is running: `services.msc` → SQL Server service
- Confirm server name: `SELECT @@SERVERNAME;` in SSMS
- Check firewall if remote server

### Issue: Stored procedure not found

**Solution**:
- Verify procedure exists: `SELECT * FROM INFORMATION_SCHEMA.ROUTINES`
- Check database name in connection string
- Re-run 03_CreateStoredProcedures.sql

### Issue: NuGet restore fails

**Solution**:
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore
dotnet restore
```

### Issue: "Data binding operation does not have Source" in WPF

**Solution**:
- Verify ViewModel assigned to View's DataContext
- Check `[ObservableProperty]` attributes present on changed properties
- Use `INotifyPropertyChanged` if not using MVVM Toolkit

## Database Maintenance

### Add New Stored Procedure

1. Create SQL file in `Database/StoredProcedures/` with naming convention `sp_ProcedureName.sql`
2. Execute in SSMS/SQLCMD
3. Reference in code via `context.FromSqlRaw("EXEC sp_ProcedureName @param1 = {0}", value1)`

### Update Database Schema

1. Modify entity model in EF
2. Create migration: `Add-Migration DescriptiveNameHere`
3. Review generated migration in `Data/Migrations/`
4. Apply: `Update-Database`

### Seed Test Data

Edit `05_SeedData.sql` and execute:
```powershell
sqlcmd -S <server> -d RestaurantOrderManagement -i Database/05_SeedData.sql
```

## Performance Optimization

### For Slow Queries

1. Check execution plan in SSMS (Ctrl+L in query editor)
2. Add indexes as needed (see `04_CreateIndexes.sql`)
3. Consider stored procedure for complex query
4. Use `AsNoTracking()` in EF queries when updates not needed

### For Slow UI

1. Use `AsNoTracking()` for read-only queries
2. Implement pagination for large result sets
3. Cache static data (categories, allergens)
4. Load images asynchronously in separate thread

## Resources

- **Entity Framework Core Documentation**: https://docs.microsoft.com/en-us/ef/core/
- **WPF Data Binding**: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/
- **MVVM Toolkit**: https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/
- **SQL Server Stored Procedures**: https://docs.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql

## Next Steps

1. Review [data-model.md](data-model.md) for database schema details
2. Review [service-contracts.md](contracts/service-contracts.md) for service interfaces
3. Review [research.md](research.md) for technology decision rationale
4. Begin implementation following the project structure
5. Reference [plan.md](plan.md) for overall architecture approach
