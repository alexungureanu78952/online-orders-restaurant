# Stored Procedures Contract Specification

**Date**: May 5, 2026  
**Purpose**: Define all stored procedures with parameters, return schemas, and usage examples

## Overview

All data operations in RestaurantOrderManagement use parameterized SQL queries or stored procedures executed through Entity Framework Core's `FromSqlRaw` and `ExecuteSqlAsync` methods. This document specifies all 20+ stored procedures with their parameter lists, return schemas, and example usage patterns.

---

## Parameterization Standards

- **All parameters are parameterized** using EF Core `{0}`, `{1}`, etc. placeholders or Direct SQL Parameters
- **Never use string concatenation** for SQL construction
- **Return types**: Typically result sets (tables) or scalar values (identity/count)
- **Error handling**: Stored procedures may throw exceptions caught by calling code

---

## Category Stored Procedures

### sp_GetCategories
**Purpose**: Retrieve all active categories for menu display

**Parameters**: None

**Return Schema**:
```
CategoryId (INT)
Name (NVARCHAR(100))
Description (NVARCHAR(500))
IsActive (BIT)
CreatedDate (DATETIME)
```

**Example Usage (EF Core)**:
```csharp
var categories = await _context.Categories
    .FromSqlRaw("EXEC dbo.sp_GetCategories")
    .ToListAsync();
```

---

## Product Stored Procedures

### sp_GetProductById
**Purpose**: Retrieve single product with allergen list by ID

**Parameters**:
- `@ProductId` (INT, required): Product identifier

**Return Schema** (Product):
```
ProductId (INT)
CategoryId (INT)
Name (NVARCHAR(150))
Description (NVARCHAR(1000))
Price (DECIMAL(10,2))
PortionQuantity (INT)
TotalQuantity (INT)
IsAvailable (BIT)
CreatedDate (DATETIME)
ModifiedDate (DATETIME)
CategoryName (NVARCHAR(100))
```

**Return Schema** (Allergens - 2nd result set):
```
AllergenId (INT)
Name (NVARCHAR(100))
Description (NVARCHAR(500))
```

**Example Usage**:
```csharp
var product = await _context.Products
    .FromSqlRaw("EXEC dbo.sp_GetProductById @ProductId = {0}", productId)
    .FirstOrDefaultAsync();
```

---

### sp_GetProductsByCategory
**Purpose**: Retrieve all available products in a category

**Parameters**:
- `@CategoryId` (INT, required): Category identifier

**Return Schema**:
```
ProductId (INT)
CategoryId (INT)
Name (NVARCHAR(150))
Description (NVARCHAR(1000))
Price (DECIMAL(10,2))
PortionQuantity (INT)
TotalQuantity (INT)
IsAvailable (BIT)
CreatedDate (DATETIME)
ModifiedDate (DATETIME)
CategoryName (NVARCHAR(100))
```

**Example Usage**:
```csharp
var products = await _context.Products
    .FromSqlRaw("EXEC dbo.sp_GetProductsByCategory @CategoryId = {0}", categoryId)
    .ToListAsync();
```

---

### sp_SearchProducts
**Purpose**: Search products by keyword and allergen filters (case-insensitive)

**Parameters**:
- `@Keyword` (NVARCHAR(150), optional): Search term (matches Name or Description)
- `@IncludeAllergens` (NVARCHAR(MAX), optional): Comma-separated allergen IDs to INCLUDE
- `@ExcludeAllergens` (NVARCHAR(MAX), optional): Comma-separated allergen IDs to EXCLUDE

**Return Schema**:
```
ProductId (INT)
CategoryId (INT)
Name (NVARCHAR(150))
Description (NVARCHAR(1000))
Price (DECIMAL(10,2))
PortionQuantity (INT)
TotalQuantity (INT)
IsAvailable (BIT)
CategoryName (NVARCHAR(100))
```

**Example Usage**:
```csharp
var results = await _context.Products
    .FromSqlRaw("EXEC dbo.sp_SearchProducts @Keyword = {0}, @IncludeAllergens = {1}, @ExcludeAllergens = {2}",
        keyword, includeAllergenIds, excludeAllergenIds)
    .ToListAsync();
```

---

### sp_CreateProduct
**Purpose**: Insert new product and return ProductId

**Parameters**:
- `@CategoryId` (INT, required)
- `@Name` (NVARCHAR(150), required)
- `@Description` (NVARCHAR(1000), optional)
- `@Price` (DECIMAL(10,2), required): Must be > 0
- `@PortionQuantity` (INT, required): Grams, must be > 0
- `@TotalQuantity` (INT, optional): Defaults to 0

**Return**: `ProductId` (INT) - Identity value of new product

**Example Usage**:
```csharp
var productId = await _context.Database.ExecuteScalarAsync(
    "EXEC dbo.sp_CreateProduct @CategoryId = {0}, @Name = {1}, @Description = {2}, @Price = {3}, @PortionQuantity = {4}, @TotalQuantity = {5}",
    categoryId, name, description, price, portionQuantity, totalQuantity);
```

---

### sp_UpdateProduct
**Purpose**: Update existing product

**Parameters**:
- `@ProductId` (INT, required)
- `@Name` (NVARCHAR(150), required)
- `@Description` (NVARCHAR(1000), optional)
- `@Price` (DECIMAL(10,2), required)
- `@PortionQuantity` (INT, required)
- `@IsAvailable` (BIT, required)

**Return**: None (rows affected by UPDATE statement)

**Example Usage**:
```csharp
await _context.Database.ExecuteAsync(
    "EXEC dbo.sp_UpdateProduct @ProductId = {0}, @Name = {1}, @Description = {2}, @Price = {3}, @PortionQuantity = {4}, @IsAvailable = {5}",
    productId, name, description, price, portionQuantity, isAvailable);
```

---

### sp_DeleteProduct
**Purpose**: Soft-delete product (sets IsDeleted = 1, IsAvailable = 0)

**Parameters**:
- `@ProductId` (INT, required)

**Return**: None (rows affected)

---

### sp_GetLowStockProducts
**Purpose**: Retrieve products below configured low-stock threshold

**Parameters**: None (reads threshold from Configuration table)

**Return Schema**:
```
ProductId (INT)
Name (NVARCHAR(150))
TotalQuantity (INT)
CategoryName (NVARCHAR(100))
Threshold (INT)
```

---

### sp_UpdateInventory
**Purpose**: Adjust product stock quantity (for orders, restocking)

**Parameters**:
- `@ProductId` (INT, required)
- `@QuantityChange` (INT, required): Negative for decrease, positive for restock

**Return**: None (updates IsAvailable based on new TotalQuantity)

---

## User Stored Procedures

### sp_CreateUser
**Purpose**: Register new user (client or employee)

**Parameters**:
- `@Email` (NVARCHAR(255), required): Must be unique
- `@PasswordHash` (NVARCHAR(255), required): bcrypt hash, never plaintext
- `@FirstName` (NVARCHAR(100), required)
- `@LastName` (NVARCHAR(100), required)
- `@PhoneNumber` (NVARCHAR(20), optional)
- `@DeliveryAddress` (NVARCHAR(500), optional)
- `@Role` (NVARCHAR(20), optional): Defaults to 'Client'

**Return**: `UserId` (INT) - Identity value  
**Error**: Throws if email already exists (unique constraint)

---

### sp_GetUserByEmail
**Purpose**: Retrieve user for authentication

**Parameters**:
- `@Email` (NVARCHAR(255), required)

**Return Schema**:
```
UserId (INT)
Email (NVARCHAR(255))
PasswordHash (NVARCHAR(255))
FirstName (NVARCHAR(100))
LastName (NVARCHAR(100))
PhoneNumber (NVARCHAR(20))
DeliveryAddress (NVARCHAR(500))
Role (NVARCHAR(20))
IsActive (BIT)
LastLoginDate (DATETIME)
```

---

### sp_UpdateLastLoginDate
**Purpose**: Update user's last login timestamp

**Parameters**:
- `@UserId` (INT, required)

**Return**: None (rows affected)

---

## Order Stored Procedures

### sp_CreateOrder
**Purpose**: Create new order and generate OrderCode

**Parameters**:
- `@UserId` (INT, required)
- `@SubTotal` (DECIMAL(10,2), required): Sum of items before fees/discounts
- `@ShippingFee` (DECIMAL(10,2), optional): Defaults to 0
- `@DiscountAmount` (DECIMAL(10,2), optional): Defaults to 0
- `@DeliveryAddress` (NVARCHAR(500), optional): Overrides user's default
- `@Notes` (NVARCHAR(1000), optional)

**Return Schema**:
```
OrderId (INT)
OrderCode (NVARCHAR(20))
```

**Business Logic**:
- GeneratesOrderCode format: `ORD-YYYYMMDD-XXXXX`
- TotalCost = SubTotal + ShippingFee - DiscountAmount
- EstimatedDeliveryTime = NOW() + 30 minutes
- Status defaults to 'inregistrata'

---

### sp_GetOrderDetails
**Purpose**: Retrieve order header and line items

**Parameters**:
- `@OrderId` (INT, required)

**Return Schema** (Order):
```
OrderId (INT)
UserId (INT)
OrderCode (NVARCHAR(20))
OrderDate (DATETIME)
Status (NVARCHAR(20))
SubTotal (DECIMAL(10,2))
ShippingFee (DECIMAL(10,2))
DiscountAmount (DECIMAL(10,2))
TotalCost (DECIMAL(10,2))
EstimatedDeliveryTime (DATETIME)
ActualDeliveryTime (DATETIME)
DeliveryAddress (NVARCHAR(500))
Notes (NVARCHAR(1000))
FirstName (NVARCHAR(100))
LastName (NVARCHAR(100))
Email (NVARCHAR(255))
```

**Return Schema** (OrderItems - 2nd result set):
```
OrderItemId (INT)
ProductId (INT)
Quantity (INT)
UnitPrice (DECIMAL(10,2))
ItemTotal (DECIMAL(10,2))
ProductName (NVARCHAR(150))
```

---

### sp_GetUserOrders
**Purpose**: Retrieve all orders for authenticated user (recent first)

**Parameters**:
- `@UserId` (INT, required)
- `@Limit` (INT, optional): Defaults to 50

**Return Schema**:
```
OrderId (INT)
OrderCode (NVARCHAR(20))
OrderDate (DATETIME)
Status (NVARCHAR(20))
SubTotal (DECIMAL(10,2))
ShippingFee (DECIMAL(10,2))
DiscountAmount (DECIMAL(10,2))
TotalCost (DECIMAL(10,2))
EstimatedDeliveryTime (DATETIME)
ActualDeliveryTime (DATETIME)
```

---

### sp_GetAllOrders
**Purpose**: Retrieve orders for admin dashboard (optional filters)

**Parameters**:
- `@Status` (NVARCHAR(20), optional): Filter by status
- `@FromDate` (DATETIME, optional): Filter orders >= this date
- `@ToDate` (DATETIME, optional): Filter orders <= this date
- `@Limit` (INT, optional): Defaults to 100

**Return Schema**:
```
OrderId (INT)
OrderCode (NVARCHAR(20))
OrderDate (DATETIME)
Status (NVARCHAR(20))
SubTotal (DECIMAL(10,2))
ShippingFee (DECIMAL(10,2))
DiscountAmount (DECIMAL(10,2))
TotalCost (DECIMAL(10,2))
FirstName (NVARCHAR(100))
LastName (NVARCHAR(100))
Email (NVARCHAR(255))
```

---

### sp_UpdateOrderStatus
**Purpose**: Update order status with validation

**Parameters**:
- `@OrderId` (INT, required)
- `@NewStatus` (NVARCHAR(20), required): One of: 'inregistrata', 'se pregateste', 'a plecat la client', 'livrata', 'anulata'

**Business Logic**:
- Prevents status changes from terminal states ('livrata', 'anulata')
- Sets ActualDeliveryTime = NOW() when status = 'livrata'
- Throws error if order is already completed/cancelled

---

## Configuration Stored Procedures

### sp_GetConfiguration
**Purpose**: Retrieve current configuration settings

**Parameters**: None

**Return Schema**:
```
ConfigId (INT)
MinOrderForFreeShipping (DECIMAL(10,2))
ShippingFee (DECIMAL(10,2))
LargeOrderDiscountThreshold (DECIMAL(10,2))
LargeOrderDiscountPercent (DECIMAL(5,2))
MenuBundleDiscountPercent (DECIMAL(5,2))
FrequentOrderThreshold (INT)
FrequentOrderTimeWindow (INT)
FrequentOrderDiscountPercent (DECIMAL(5,2))
LowStockThreshold (INT)
```

---

### sp_UpdateConfiguration
**Purpose**: Update configuration settings

**Parameters** (all optional, NULL = don't change):
- `@MinOrderForFreeShipping` (DECIMAL(10,2))
- `@ShippingFee` (DECIMAL(10,2))
- `@LargeOrderDiscountThreshold` (DECIMAL(10,2))
- `@LargeOrderDiscountPercent` (DECIMAL(5,2))
- `@LowStockThreshold` (INT)

**Return**: None

---

## Security & Parameterization

### All Procedures Use Parameterized Execution

✅ **CORRECT** (Parameterized):
```csharp
await _context.Products
    .FromSqlRaw("EXEC dbo.sp_GetProductsByCategory @CategoryId = {0}", categoryId)
    .ToListAsync();
```

❌ **INCORRECT** (String concatenation - NEVER USE):
```csharp
// DO NOT DO THIS - SQL INJECTION VULNERABILITY
var sql = "EXEC dbo.sp_GetProductsByCategory @CategoryId = " + categoryId;
```

### Testing SQL Injection Prevention
See [SQL Injection Tests](../tests/Security/SqlInjectionTests.cs) for validation that all procedures reject injection attempts.

---

## Summary

- **Total Procedures**: 20+
- **All parameterized**: 100%
- **Error handling**: Handled at application layer
- **Testing**: Full coverage in unit/integration tests
