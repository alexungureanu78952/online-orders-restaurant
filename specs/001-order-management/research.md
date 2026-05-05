# Research & Technology Decisions: Restaurant Order Management System

**Date**: May 5, 2026
**Purpose**: Document technology research, design patterns, and best practices for implementation

## 1. Entity Framework Core Integration

**Decision**: Use Entity Framework Core 6+ with Code-First migrations, combined with stored procedures for complex operations

**Rationale**: 
- Entity Framework enables MVVM-friendly lazy loading and change tracking
- Code-First migrations version database schema with source code
- Supports LINQ for type-safe queries in services
- Can integrate stored procedures for performance-critical operations (required by spec: 10+ SPs)
- Built-in parameterization prevents SQL injection automatically
- Works seamlessly with dependency injection for testability

**Alternatives Considered**:
- Dapper (lightweight but requires manual SP mapping and more boilerplate)
- Raw ADO.NET (full control but highest maintenance and injection risk)
- Entity Framework 6 (legacy; Core is industry standard for new projects)

**Implementation Pattern**: 
- Use DbContext with DbSet<T> for standard CRUD
- Map stored procedures using `context.FromSqlRaw()` for complex queries
- DbSets configured with relationships for automatic navigation
- Migrations tracked in `Migrations/` folder

---

## 2. MVVM Architecture & Data Binding

**Decision**: Implement full MVVM with WPF data binding, using MVVM Toolkit for base classes and Prism for DI

**Rationale**:
- Requirement explicitly mandates MVVM architecture and data binding
- Separates UI concerns from business logic (testable ViewModels)
- Data binding eliminates boilerplate code for UI updates
- MVVM Toolkit provides `ObservableObject`, `RelayCommand` base classes
- Prism provides ServiceCollection for dependency injection

**Alternatives Considered**:
- Code-behind logic (violates MVVM requirement, difficult to test)
- Manual notification implementation (error-prone, verbose)

**Implementation Pattern**:
```csharp
// BaseViewModel with INotifyPropertyChanged
public partial class MenuBrowseViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> categories;
    
    [RelayCommand]
    private void SelectCategory(CategoryViewModel category) { }
}

// XAML binding (no code-behind)
<ItemsControl ItemsSource="{Binding Categories}" />
```

---

## 3. Stored Procedures Architecture

**Decision**: Implement 10+ stored procedures covering CRUD operations with dedicated files in `Data/StoredProcedures/`

**Rationale**:
- Specification requirement: minimum 10 SPs (2+ inserts, 2+ updates, 2+ selects)
- Procedures allow centralized business logic validation in database layer
- Performance optimization for complex queries (e.g., order totals with discounts)
- Stored procedures + Entity Framework: use `FromSqlRaw()` with parameters

**Procedures to Implement** (13 minimum):
- Insert: `sp_CreateOrder`, `sp_InsertProduct` (2+)
- Update: `sp_UpdateOrderStatus`, `sp_UpdateInventory` (2+)
- Select: `sp_GetProductsByCategory`, `sp_SearchProducts`, `sp_GetOrderDetails`, `sp_GetUserOrders`, `sp_GetAllOrders`, `sp_GetLowStockProducts` (6+)
- Complex: `sp_ApplyOrderDiscounts`, `sp_ValidateOrderInventory`

**SQL Injection Prevention**: All parameters passed as `SqlParameter` objects with typed values (never string concatenation)

---

## 4. Configuration Management

**Decision**: External XML configuration file (`RestaurantConfig.xml`) for business parameters, loaded at startup

**Rationale**:
- Specification requires: discounts, fees, thresholds, time periods read from configuration file
- Enables runtime changes without code recompilation
- Centralized source of truth for business rules
- Configuration service validates format on startup

**Configuration Parameters**:
```xml
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

---

## 5. Database Normalization

**Decision**: Implement database schema in Third Normal Form (3NF) with carefully designed relationships

**Rationale**:
- Specification requirement: "Database must be in 3NF form (-2p penalty if violated)"
- 3NF eliminates transitive dependencies and redundant data
- Ensures data integrity and consistency
- Supports efficient indexing for performance

**Key Normalization Decisions**:
- Separate `Product` and `Menu` entities (both reference categories, preventing transitive dependency)
- Junction tables for many-to-many: `ProductAllergen` (Product ↔ Allergen), `MenuProduct` (Menu ↔ Product)
- Separate `OrderItem` table from `Order` to track per-product quantities and prices at order time
- `ProductImage` separate table to avoid repeating image URLs in Product
- Configuration values in dedicated `Configuration` table (alternative: external file)

**Functional Dependencies**:
- `Product.portion_quantity` and `Product.total_quantity` are independent (no transitive dependency from category)
- All non-key attributes depend on primary key only (no partial dependencies)

---

## 6. User Authentication & Password Security

**Decision**: Email/password authentication with bcrypt hashing, no plaintext storage

**Rationale**:
- Requirement: authenticate via email + password
- bcrypt automatically handles salt generation and provides configurable work factor
- Prevents rainbow table attacks
- Industry standard for credential storage

**Implementation**:
- Use `BCrypt.Net` NuGet package
- Store only bcrypt hash in `User.password_hash`
- Never store plaintext passwords
- Never display hash values in UI

---

## 7. Order Inventory Updates

**Decision**: Inventory updates happen automatically on order delivery; inventory restored on order cancellation

**Rationale**:
- Specification: "At delivery, inventory automatically updated; on cancellation, restored"
- Ensures inventory accuracy matches physical stock
- Prevents overselling (if order cannot be fulfilled)
- Stored procedure `sp_UpdateInventory` called atomically with status update

**Flow**:
```
1. Order placed: inventory unchanged (order in "inregistrata" state)
2. Order status → "livrata": sp_UpdateInventory called, quantities reduced
3. Order status → "anulata": quantities restored
```

---

## 8. Discount Logic

**Decision**: Automatic discounts applied based on two criteria; discount priority handled in service layer

**Rationale**:
- Specification defines two discount scenarios:
  1. Order exceeds configured amount
  2. Customer has ordered N times in last T days
- Applied automatically, no manual intervention needed
- Service layer encapsulates business rules for testability

**Implementation**:
```csharp
public decimal CalculateDiscount(Order order, User customer)
{
    decimal discount = 0;
    if (order.SubTotal > config.LargeOrderThreshold)
        discount = Math.Max(discount, config.LargeOrderDiscountPercent);
    
    var recentOrders = GetCustomerOrdersInPeriod(customer, config.TimeWindow);
    if (recentOrders.Count >= config.OrderThreshold)
        discount = Math.Max(discount, config.FrequentCustomerDiscountPercent);
    
    return discount;
}
```

---

## 9. Search & Filtering

**Decision**: Implement search with case-insensitive LIKE pattern; allergen filtering via INNER JOIN with ProductAllergen

**Rationale**:
- Specification requires: search by keyword, filter by allergen presence/absence
- Case-insensitive LIKE pattern: `UPPER(Product.Name) LIKE @search`
- Allergen inclusion: `INNER JOIN ProductAllergen WHERE Allergen.Name = @allergen`
- Allergen exclusion: `WHERE Product.Id NOT IN (SELECT ProductId FROM ProductAllergen WHERE ...)`

**Stored Procedure**:
```sql
CREATE PROCEDURE sp_SearchProducts
    @keyword NVARCHAR(100),
    @allergen NVARCHAR(100) = NULL,
    @includeAllergen BIT = 1
AS
SELECT DISTINCT p.* FROM Product p
WHERE UPPER(p.Name) LIKE UPPER('%' + @keyword + '%')
  AND (@allergen IS NULL 
       OR (@includeAllergen = 1 
           AND EXISTS (SELECT 1 FROM ProductAllergen pa 
                       JOIN Allergen a ON pa.AllergenId = a.Id
                       WHERE pa.ProductId = p.Id AND a.Name = @allergen))
       OR (@includeAllergen = 0 
           AND NOT EXISTS (SELECT 1 FROM ProductAllergen pa 
                           JOIN Allergen a ON pa.AllergenId = a.Id
                           WHERE pa.ProductId = p.Id AND a.Name = @allergen)))
```

---

## 10. Testing Strategy

**Decision**: Unit tests for services and ViewModels; integration tests for stored procedures; no UI testing

**Rationale**:
- Services and ViewModels are pure C# (easily testable with xUnit + Moq)
- StoredProcedures tested via integration tests against test database
- WPF UI testing is expensive and brittle; data binding and layout tested manually
- Focus testing effort on business logic (discount calculations, order processing)

**Test Structure**:
- `AuthenticationServiceTests`: login validation, registration validation
- `OrderServiceTests`: discount application, inventory updates, order status transitions
- `ProductServiceTests`: search filtering, category organization
- `RepositoryTests`: CRUD operations, stored procedure calls
- `ViewModelTests`: command execution, property changes, data binding setup

---

## 11. Id Value Hiding

**Decision**: Database IDs stored and used internally; UI displays only business identifiers (e.g., OrderCode, not OrderId)

**Rationale**:
- Specification requirement: "No table ID values displayed in UI (-1p penalty if violated)"
- Order code (unique, customer-facing): displayed to users
- Category names (already unique keys): displayed instead of category IDs
- Product names: displayed instead of product IDs
- Business identifiers provide security (prevents ID enumeration attacks)

**Implementation**:
- ViewModel properties expose only business values (no Id properties)
- OrderCode in Order table serves as customer-facing identifier
- Database lookups use internal IDs (EF Core handles this transparently)

---

## 12. Layered Architecture Implementation

**Decision**: Strict 3-layer architecture (Presentation → Services → Data) with dependency injection

**Rationale**:
- Specification requirement: "Layered structure required (-1p if missing)"
- Supports independent testing of each layer
- Prevents circular dependencies
- Clear separation of concerns

**Layer Responsibilities**:
- **Presentation (WPF)**: ViewModels, Views, Converters — UI logic only, no business logic
- **Services**: All business logic, validation, orchestration — depends on Data layer only
- **Data**: Entity Framework, repositories, stored procedures — no business logic

**DI Setup**:
```csharp
services.AddScoped<IProductService, ProductService>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IRepository<Product>, GenericRepository<Product>>();
services.AddDbContext<RestaurantDbContext>(options => 
    options.UseSqlServer(connectionString));
```

---

## Summary

All technology decisions align with specification requirements and industry best practices for WPF applications. The MVVM + Entity Framework + Stored Procedures approach balances developer productivity with performance and maintainability, while strict adherence to 3NF, parameter binding, and architecture layering ensures code quality and security.
