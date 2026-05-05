# Implementation Plan: Restaurant Order Management System

**Branch**: `001-order-management` | **Date**: May 5, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification for WPF desktop application with .NET C# and SQL Server

## Summary

Build a desktop restaurant order management application enabling customers to browse menu, search products, place orders online, and manage order history, while providing staff with order fulfillment, inventory tracking, and menu management capabilities. 

**Technical Approach**: Multi-layer MVVM architecture with WPF frontend binding to ViewModels, business logic layer with Entity Framework Core for data access, and SQL Server database in 3NF with stored procedures for all data operations.

## Technical Context

**Language/Version**: C# 10+ (.NET 6.0 or later)  
**Primary Dependencies**: 
- WPF (Windows Presentation Foundation) for UI
- Entity Framework Core 6+ for ORM and database access
- Entity Framework Power Tools for code generation
- MVVM Toolkit for MVVM pattern support
- Prism or similar for dependency injection
- SQL Server 2019+ or Azure SQL for database
- log4net or Serilog for logging

**Storage**: SQL Server database in Third Normal Form (3NF) with minimum 10 stored procedures
**Testing**: xUnit or NUnit for unit tests; Moq for mocking  
**Target Platform**: Windows 10/11 desktop (.NET Framework / .NET Core)
**Project Type**: Desktop application (multi-user, single location)  
**Performance Goals**: 
- Menu load < 1 second
- Search results < 500ms
- Order submission < 2 seconds
- Support 5-10 concurrent users
- 100-500 active customers
- 50-200 concurrent orders

**Constraints**: 
- SQL Injection prevention mandatory on all parameterized queries
- No table ID values displayed in UI
- Configuration file-driven (discounts, fees, thresholds)
- Standard Windows desktop deployment
- No complex encryption required (password hashing sufficient)

**Scale/Scope**: 
- 8-10 main screens (Menu Browse, Search, Order Cart, Order History, Admin Dashboard, etc.)
- 3 user roles (Unauthenticated, Client, Employee)
- ~15-20 database tables
- ~2000-3000 lines of business logic

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: Constitution not yet established for project. Proceeding with specification requirements as gates.

## Project Structure

### Documentation (this feature)

```text
specs/001-order-management/
├── plan.md              # This file (implementation planning)
├── research.md          # Phase 0 output - technology & pattern research
├── data-model.md        # Phase 1 output - database schema & entities
├── quickstart.md        # Phase 1 output - setup and development guide
├── contracts/           # Phase 1 output - API/contract definitions
│   ├── domain-models.md
│   ├── service-contracts.md
│   └── stored-procedures.md
└── tasks.md             # Phase 2 output - actionable implementation tasks
```

### Source Code (repository root) - MVVM Desktop Architecture

```text
RestaurantOrderManagement/
├── RestaurantOrderManagement.sln         # Visual Studio solution

# Presentation Layer - WPF UI
├── RestaurantOrderManagement.WPF/
│   ├── App.xaml & App.xaml.cs
│   ├── MainWindow.xaml & MainWindow.xaml.cs
│   ├── ViewModels/
│   │   ├── BaseViewModel.cs              # Base MVVM class
│   │   ├── MenuBrowseViewModel.cs
│   │   ├── SearchViewModel.cs
│   │   ├── OrderCartViewModel.cs
│   │   ├── OrderHistoryViewModel.cs
│   │   ├── LoginViewModel.cs
│   │   ├── RegistrationViewModel.cs
│   │   ├── AdminDashboardViewModel.cs
│   │   ├── InventoryViewModel.cs
│   │   └── OrderManagementViewModel.cs
│   ├── Views/
│   │   ├── MenuBrowseView.xaml
│   │   ├── SearchView.xaml
│   │   ├── OrderCartView.xaml
│   │   ├── OrderHistoryView.xaml
│   │   ├── LoginView.xaml
│   │   ├── RegistrationView.xaml
│   │   ├── AdminDashboardView.xaml
│   │   ├── InventoryView.xaml
│   │   └── OrderManagementView.xaml
│   ├── Converters/
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── OrderStatusColorConverter.cs
│   │   └── PriceFormattingConverter.cs
│   ├── Resources/
│   │   └── Styles.xaml
│   └── RestaurantOrderManagement.WPF.csproj

# Business Logic Layer
├── RestaurantOrderManagement.Services/
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── IProductService.cs
│   │   ├── IOrderService.cs
│   │   ├── IInventoryService.cs
│   │   └── IConfigurationService.cs
│   ├── Implementations/
│   │   ├── AuthenticationService.cs
│   │   ├── ProductService.cs
│   │   ├── OrderService.cs
│   │   ├── InventoryService.cs
│   │   └── ConfigurationService.cs
│   └── RestaurantOrderManagement.Services.csproj

# Data Access Layer - Entity Framework
├── RestaurantOrderManagement.Data/
│   ├── Context/
│   │   └── RestaurantDbContext.cs
│   ├── Models/
│   │   ├── Category.cs
│   │   ├── Product.cs
│   │   ├── ProductImage.cs
│   │   ├── Menu.cs
│   │   ├── MenuProduct.cs
│   │   ├── Allergen.cs
│   │   ├── ProductAllergen.cs
│   │   ├── User.cs
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── Configuration.cs
│   │   └── Enums.cs
│   ├── Migrations/
│   │   ├── Initial_Schema.cs
│   │   └── [other migrations]
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── GenericRepository.cs
│   │   ├── ProductRepository.cs
│   │   ├── OrderRepository.cs
│   │   └── UserRepository.cs
│   ├── StoredProcedures/
│   │   ├── sp_GetProductsByCategory.sql
│   │   ├── sp_SearchProducts.sql
│   │   ├── sp_CreateOrder.sql
│   │   ├── sp_GetOrderDetails.sql
│   │   ├── sp_UpdateOrderStatus.sql
│   │   ├── sp_UpdateInventory.sql
│   │   ├── sp_GetLowStockProducts.sql
│   │   ├── sp_ApplyOrderDiscounts.sql
│   │   ├── sp_GetUserOrders.sql
│   │   └── sp_GetAllOrders.sql
│   └── RestaurantOrderManagement.Data.csproj

# Unit Tests
├── RestaurantOrderManagement.Tests/
│   ├── Services/
│   │   ├── AuthenticationServiceTests.cs
│   │   ├── ProductServiceTests.cs
│   │   ├── OrderServiceTests.cs
│   │   └── InventoryServiceTests.cs
│   ├── ViewModels/
│   │   ├── MenuBrowseViewModelTests.cs
│   │   ├── OrderCartViewModelTests.cs
│   │   └── SearchViewModelTests.cs
│   ├── Data/
│   │   ├── RepositoryTests.cs
│   │   └── DbContextTests.cs
│   └── RestaurantOrderManagement.Tests.csproj

# Configuration Files
├── app.config                           # Application configuration (connection strings, settings)
├── RestaurantConfig.xml                 # Business configuration (discounts, fees, thresholds)
├── appsettings.json                     # .NET configuration (logging, etc)

# Database Setup
└── Database/
    ├── 01_CreateSchema.sql              # Initial schema creation
    ├── 02_CreateTables.sql              # All table definitions
    ├── 03_CreateStoredProcedures.sql    # All 10+ stored procedures
    ├── 04_CreateIndexes.sql             # Performance indexes
    ├── 05_SeedData.sql                  # Initial categories, allergens, etc
    └── README.md                        # Database setup instructions
```

**Structure Decision**: Standard MVVM 3-layer architecture for WPF:
- **Presentation Layer** (WPF): Views + ViewModels with data binding
- **Business Logic Layer** (Services): Domain logic, validation, orchestration
- **Data Access Layer** (Entity Framework + Repositories): Database operations via EF Core with stored procedures
- **Cross-cutting**: Configuration, logging, dependency injection

This structure enables:
- Testable ViewModels (no UI dependencies)
- Reusable services (can be unit tested independently)
- Clean separation of concerns
- Compliance with MVVM architecture requirement

## Complexity Tracking

No Constitutional violations to justify at this stage. All requirements are met by standard MVVM + EF Core architecture.
