---
description: "Tasks for 001-order-management: Restaurant Order Management System"
---

# Tasks: Restaurant Order Management (001-order-management)

**Input**: plan.md, spec.md, data-model.md, contracts/
**Deadline**: May 18-22, 2026

## Notes
- All user-story tasks use MVVM (Views in RestaurantOrderManagement.WPF/Views, ViewModels in RestaurantOrderManagement.WPF/ViewModels)
- Data access uses Entity Framework Core in RestaurantOrderManagement.Data with stored procedures under Database/03_CreateStoredProcedures.sql
- 3NF validation tasks included in Foundational phase
- All DB queries must use parameterized queries / EF parameter binding
- UI must never display internal integer IDs — use `OrderCode` or natural keys

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create solution and projects per plan: RestaurantOrderManagement.sln and the projects at repository root (Presentation, Services, Data, Tests) (RestaurantOrderManagement.sln)
- [X] T002 Initialize `RestaurantOrderManagement.WPF` project with MVVM Toolkit and configure App.xaml, MainWindow (RestaurantOrderManagement.WPF/)
- [X] T003 Initialize `RestaurantOrderManagement.Data` project with EF Core, create `RestaurantDbContext.cs` skeleton (RestaurantOrderManagement.Data/Context/RestaurantDbContext.cs)
- [X] T004 Add `RestaurantOrderManagement.Services` project and create Interfaces folder with service interfaces skeleton (RestaurantOrderManagement.Services/Interfaces/)
- [X] T005 Add CI-friendly config, logging, and DI bootstrap: configure `appsettings.json`, Serilog, and DI registration placeholder (RestaurantOrderManagement.WPF/App.xaml.cs, RestaurantOrderManagement.Services/Startup.cs)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Database, schema, stored procedures, EF integration, repositories, authentication, configuration, and 3NF validation. Complete before implementing user stories.

- [X] T006 Create database schema scripts in Database/02_CreateTables.sql following `specs/001-order-management/data-model.md` (Database/02_CreateTables.sql)
- [X] T007 Implement migrations and seed data using EF Core migrations and add `Initial_Schema` migration (RestaurantOrderManagement.Data/Migrations/Initial_Schema.cs)
- [X] T008 Design and add stored procedure scripts (minimum 10) in Database/03_CreateStoredProcedures.sql: include at least these procedures: sp_CreateProduct, sp_UpdateProduct, sp_DeleteProduct, sp_GetProductById, sp_GetProductsByCategory, sp_SearchProducts, sp_CreateOrder, sp_GetOrderDetails, sp_UpdateOrderStatus, sp_UpdateInventory, sp_GetLowStockProducts, sp_GetUserOrders (Database/03_CreateStoredProcedures.sql)
- [X] T009 Implement `StoredProcedures.md` contract in specs/contracts/stored-procedures.md listing parameters, return schemas, and examples for every stored procedure (specs/001-order-management/contracts/stored-procedures.md)
- [X] T010 Implement repositories and EF wrappers that call stored procedures via parameterized `FromSqlRaw`/`ExecuteSqlRaw` (RestaurantOrderManagement.Data/Repositories/*Repository.cs)
- [X] T011 Create `IRepository` and `GenericRepository` and concrete `ProductRepository` and `OrderRepository` with methods mapped to stored procedures (RestaurantOrderManagement.Data/Repositories/)
- [X] T012 Add database unit/integration tests that validate each stored procedure's behavior and parameterization (RestaurantOrderManagement.Tests/Data/StoredProcedureTests.cs)
- [X] T013 Validate 3NF compliance with a documented checklist and SQL constraints; add `3NF-verification.md` in specs (specs/001-order-management/checklists/3NF-verification.md)
- [X] T014 Implement secure parameterized query patterns and add a static analyzer / code-review checklist item to enforce no string-concatenated SQL (RestaurantOrderManagement.Data/CodeReview/ParameterizedQueriesChecklist.md)
- [X] T015 Implement configuration store and loader: `Configuration` entity, `RestaurantConfig.xml` sample, and `ConfigurationService` that caches settings on startup (RestaurantOrderManagement.Data/Models/Configuration.cs, RestaurantOrderManagement.Services/Implementations/ConfigurationService.cs)
- [X] T016 Implement authentication scaffolding (IAuthenticationService + AuthenticationService) with hashed passwords and role enforcement (RestaurantOrderManagement.Services/Implementations/AuthenticationService.cs)
- [X] T017 Create a SQL injection test suite that attempts common injection patterns against repository parameterized calls and ensure they are blocked (RestaurantOrderManagement.Tests/Security/SqlInjectionTests.cs)

---

## Phase 3: User Story 1 - Browse Restaurant Menu (Priority: P1) 🎯 MVP

**Goal**: Display categories and products with images, prices, portions, and allergen lists. Mark unavailable products and menus.
**Independent Test**: Launch WPF app, open MenuBrowseView, verify categories and product details display as specified; unavailable products are non-orderable.

- [ ] T018 [P] [US1] Create `Category` and `Product` EF models and mapping to DB tables (RestaurantOrderManagement.Data/Models/Category.cs, Product.cs)
- [ ] T019 [P] [US1] Create `ProductImage`, `Allergen`, and `ProductAllergen` models and mappings (RestaurantOrderManagement.Data/Models/)
- [ ] T020 [US1] Implement `ProductRepository.GetProductsByCategory` calling `sp_GetProductsByCategory` with parameterized category id/name (RestaurantOrderManagement.Data/Repositories/ProductRepository.cs)
- [ ] T021 [US1] Implement `ProductService` and `IProductService` that returns DTOs without internal IDs (RestaurantOrderManagement.Services/Implementations/ProductService.cs)
- [ ] T022 [US1] Implement `MenuBrowseViewModel` exposing bound collections `Categories` and `ProductsByCategory` and commands for refresh (RestaurantOrderManagement.WPF/ViewModels/MenuBrowseViewModel.cs)
- [ ] T023 [US1] Implement `MenuBrowseView.xaml` binding to `MenuBrowseViewModel` with item templates that show images, price, portion, allergens, and availability (RestaurantOrderManagement.WPF/Views/MenuBrowseView.xaml)
- [ ] T024 [US1] Add localization-friendly user-facing product code: `DisplayCode` or `SKU` (not DB integer) and ensure `Product` DTO uses it for UI (RestaurantOrderManagement.Data/Models/Product.cs)
- [ ] T025 [US1] Add unit tests for `MenuBrowseViewModel` to assert view model contains no ID fields and correctly maps service DTOs (RestaurantOrderManagement.Tests/ViewModels/MenuBrowseViewModelTests.cs)

---

## Phase 4: User Story 2 - Search and Filter Menu (Priority: P1)

**Goal**: Keyword search (case-insensitive) and allergen include/exclude filters, results grouped by category.
**Independent Test**: Use SearchView to filter by keyword and allergen; verify grouping and filter accuracy.

- [ ] T026 [P] [US2] Implement `sp_SearchProducts` stored procedure with parameters: @Keyword NVARCHAR, @IncludeAllergens NVARCHAR (CSV), @ExcludeAllergens NVARCHAR (CSV) (Database/03_CreateStoredProcedures.sql)
- [ ] T027 [US2] Implement `ProductRepository.SearchProducts` using parameterized `FromSqlRaw` mapping to `sp_SearchProducts` (RestaurantOrderManagement.Data/Repositories/ProductRepository.cs)
- [ ] T028 [US2] Implement `SearchViewModel` with `SearchQuery`, `IncludeAllergens`, `ExcludeAllergens`, and `SearchCommand` bound to UI (RestaurantOrderManagement.WPF/ViewModels/SearchViewModel.cs)
- [ ] T029 [US2] Implement `SearchView.xaml` binding to `SearchViewModel` and showing grouped results by category (RestaurantOrderManagement.WPF/Views/SearchView.xaml)
- [ ] T030 [US2] Add integration tests validating search with various filter combinations and ensuring case-insensitive search (RestaurantOrderManagement.Tests/Integration/SearchTests.cs)

---

## Phase 5: User Story 3 - Create User Account (Priority: P1)

**Goal**: Registration and login with validation and unique email enforcement.
**Independent Test**: Register a user, assert login works, and email uniqueness enforced.

- [ ] T031 [P] [US3] Implement `User` EF model and unique email constraint mapping (RestaurantOrderManagement.Data/Models/User.cs)
- [ ] T032 [US3] Implement `sp_CreateUser` and `sp_GetUserByEmail` stored procedures and add them to Database/03_CreateStoredProcedures.sql
- [ ] T033 [US3] Implement `AuthenticationService.RegisterAsync` and `LoginAsync` using parameterized stored procedures or EF with hashed passwords (RestaurantOrderManagement.Services/Implementations/AuthenticationService.cs)
- [ ] T034 [US3] Implement `RegistrationView` and `RegistrationViewModel` with data binding and password validation rules (RestaurantOrderManagement.WPF/Views/RegistrationView.xaml, ViewModels/RegistrationViewModel.cs)
- [ ] T035 [US3] Add unit tests for registration/login and duplicate email rejection (RestaurantOrderManagement.Tests/Services/AuthenticationServiceTests.cs)

---

## Phase 6: User Story 4 - Place Order (Priority: P1)

**Goal**: Authenticated clients can add products to cart, submit orders, update inventory, and receive configured discounts/fees.
**Independent Test**: Place an order as a client, verify order is created, OrderCode returned, inventory updated, and totals/discounts applied.

- [ ] T036 [US4] Implement `sp_CreateOrder` with parameters for user, items (table-valued param), shipping, discounts, and return `OrderCode` (Database/03_CreateStoredProcedures.sql)
- [ ] T037 [US4] Implement `OrderRepository.CreateOrderAsync` that calls `sp_CreateOrder` with parameterized inputs and maps returned `OrderCode` (RestaurantOrderManagement.Data/Repositories/OrderRepository.cs)
- [ ] T038 [US4] Implement `OrderService` with order composition, discount calculation using `ConfigurationService`, and inventory update orchestration (RestaurantOrderManagement.Services/Implementations/OrderService.cs)
- [ ] T039 [US4] Implement `OrderCartView` and `OrderCartViewModel` with data binding for add/remove/update quantity commands (RestaurantOrderManagement.WPF/Views/OrderCartView.xaml, ViewModels/OrderCartViewModel.cs)
- [ ] T040 [US4] Add tests to validate order totals, discount rules, shipping fee behavior, and that UI shows `OrderCode` instead of numeric `OrderId` (RestaurantOrderManagement.Tests/Services/OrderServiceTests.cs)

---

## Phase 7: User Story 5 - Track Order Status (Priority: P2)

**Goal**: Clients view order history and active orders with status and estimated delivery time; can cancel active orders.
**Independent Test**: Place orders and verify history, active list, and cancel behavior restores inventory.

- [ ] T041 [US5] Implement `sp_GetUserOrders` and `sp_GetOrderDetails` (Database/03_CreateStoredProcedures.sql)
- [ ] T042 [US5] Implement `OrderRepository.GetUserOrders` and `GetOrderDetails` using EF parameterized calls (RestaurantOrderManagement.Data/Repositories/OrderRepository.cs)
- [ ] T043 [US5] Implement `OrderHistoryViewModel` and `OrderHistoryView` with bindings for order list, details, and `CancelOrderCommand` (RestaurantOrderManagement.WPF/ViewModels/OrderHistoryViewModel.cs)
- [ ] T044 [US5] Implement `OrderService.CancelOrderAsync` to validate cancellable statuses, call `sp_UpdateOrderStatus` to set 'anulata', and restore inventory (RestaurantOrderManagement.Services/Implementations/OrderService.cs)
- [ ] T045 [US5] Add tests for cancel flow and inventory restoration (RestaurantOrderManagement.Tests/Integration/OrderCancellationTests.cs)

---

## Phase 8: User Story 6 - Manage Orders / Employee Views (Priority: P2)

**Goal**: Employees view all orders, filter active orders, update statuses, and see customer info.
**Independent Test**: Log in as employee and perform status updates visible to clients.

- [ ] T046 [P] [US6] Implement `AdminDashboardView` and `OrderManagementView` WPF views; `OrderManagementViewModel` exposes filters and bulk status update commands (RestaurantOrderManagement.WPF/Views/OrderManagementView.xaml)
- [ ] T047 [US6] Implement `sp_GetAllOrders` and `sp_UpdateOrderStatus` stored procedures (Database/03_CreateStoredProcedures.sql)
- [ ] T048 [US6] Implement `OrderRepository.GetAllOrders` and `UpdateOrderStatus` with parameterized calls (RestaurantOrderManagement.Data/Repositories/OrderRepository.cs)
- [ ] T049 [US6] Add `Employee` role checks in `AuthenticationService` and ViewModel authorization guards (RestaurantOrderManagement.Services/Implementations/AuthenticationService.cs)
- [ ] T050 [US6] Add tests verifying employee-only access to management views and that updates propagate to client views (RestaurantOrderManagement.Tests/Integration/EmployeeFlowTests.cs)

---

## Phase 9: User Story 7 - Manage Inventory (Priority: P2)

**Goal**: Employees view low-stock products, update inventory, and trigger reorder notifications.
**Independent Test**: Reduce product quantity below threshold and verify it appears in low stock list.

- [ ] T051 [US7] Implement `sp_GetLowStockProducts` stored procedure and `sp_UpdateInventory` (Database/03_CreateStoredProcedures.sql)
- [ ] T052 [US7] Implement `InventoryService` and `InventoryViewModel` to surface low-stock items and support manual restock actions (RestaurantOrderManagement.Services/Implementations/InventoryService.cs, RestaurantOrderManagement.WPF/ViewModels/InventoryViewModel.cs)
- [ ] T053 [US7] Add UI for restocking products with parameterized calls (RestaurantOrderManagement.WPF/Views/InventoryView.xaml)
- [ ] T054 [US7] Add tests for low-stock detection and restock flows (RestaurantOrderManagement.Tests/Integration/InventoryTests.cs)

---

## Phase 10: User Story 8 - Manage Menu Items (Priority: P3)

**Goal**: Employees can create/update/delete categories and products (CRUD) — satisfies grading CRUD requirement on at least 2 tables.
**Independent Test**: Create/update/delete a product and category via admin UI; verify DB changes via stored procedures.

- [ ] T055 [US8] Implement `sp_CreateProduct`, `sp_UpdateProduct`, `sp_DeleteProduct` stored procedures (Database/03_CreateStoredProcedures.sql)
- [ ] T056 [US8] Implement `ProductRepository` CRUD methods that call these stored procedures with parameterized inputs (RestaurantOrderManagement.Data/Repositories/ProductRepository.cs)
- [ ] T057 [US8] Implement `ProductManagementView` and `ProductManagementViewModel` for employee CRUD operations (RestaurantOrderManagement.WPF/Views/ProductManagementView.xaml)
- [ ] T058 [US8] Add unit and integration tests covering product & category CRUD (RestaurantOrderManagement.Tests/Integration/ProductCrudTests.cs)

---

## Phase 11: User Story 9 - Generate Reports / Office & Misc (Priority: P3)

**Goal**: Exportable reports for orders, revenue, and inventory; configuration and misc tasks for final grading.
**Independent Test**: Generate a report for a selected period and export CSV.

- [ ] T059 [US9] Implement `ReportService` that uses parameterized queries/stored procedures to generate order and revenue summaries (RestaurantOrderManagement.Services/Implementations/ReportService.cs)
- [ ] T060 [US9] Implement `ReportsView` and `ReportsViewModel` with export CSV functionality (RestaurantOrderManagement.WPF/Views/ReportsView.xaml)
- [ ] T061 [US9] Add Quickstart and README updates describing how to run DB scripts and the app (specs/001-order-management/quickstart.md)
- [ ] T062 [US9] Finalize security checklist ensuring all queries are parameterized and no IDs are shown in UI (specs/001-order-management/checklists/security-checklist.md)

---

## Phase 12: Polish & Cross-Cutting Concerns

- [ ] T063 [P] Documentation: Update `specs/001-order-management` docs with stored procedure parameter lists and API usage (specs/001-order-management/contracts/)
- [ ] T064 [P] Performance tuning: add indexes from data-model recommendations and run performance tests (Database/04_CreateIndexes.sql)
- [ ] T065 [P] Accessibility & UX polish for WPF views (RestaurantOrderManagement.WPF/Resources/Styles.xaml)
- [ ] T066 End-to-end smoke tests and release checklist (RestaurantOrderManagement.Tests/EndToEnd/SmokeTests.cs)

---

## Dependencies & Execution Order

- Setup (T001-T005) -> Foundational (T006-T017) is BLOCKING. No user-story tasks start until Foundational is complete.
- After Foundational completes, P1 stories (US1, US2, US3, US4) proceed next (T018-T040). P2/P3 follow in priority order but can run in parallel once foundation ready.
- Stored procedures (T008, T026, T036, T041, T047, T051, T055) must be authored before repository methods that call them (T010, T020, T027, T037, T042, T048, T052, T056).
- UI ViewModels must be implemented before final WPF Views are data-bound (e.g., T022 before T023).

## Summary Report

- Total tasks: 66
- Tasks mapped to grading rubric:
  - CRUD (Product, Category): covered by T055-T057 (US8)
  - Display menu: covered by T018-T025 (US1)
  - Search/filters: covered by T026-T030 (US2)
  - Employee views: covered by T046-T050 and T051-T054 (US6, US7)
  - Order management: covered by T036-T045 (US4, US5)
  - Office/misc (reports, docs): covered by T059-T061 (US9)
- Stored procedures: T008 lists 12+ required; individual creation tasks referenced (T026, T036, T041, T047, T051, T055)
- MVVM: Enforced by ViewModel-first tasks (MenuBrowseViewModel, SearchViewModel, OrderCartViewModel, etc.) throughout user stories
- 3NF validation: T013 ensures documentation and verification
- SQL injection prevention: T014 and T017 ensure parameterized queries and tests
- No IDs in UI: T024 and T040 enforce use of `OrderCode`/DisplayCode

---

## Post-generation Hooks

## Extension Hooks

**Optional Pre-Hook**: git
Command: `speckit.git.commit`
Description: Auto-commit before task generation

Prompt: Commit outstanding changes before task generation?
To execute: `speckit.git.commit`
