# Data Model: Restaurant Order Management System

**Date**: May 5, 2026
**Purpose**: Define complete database schema in Third Normal Form (3NF) with all entities, relationships, and validation rules

## Entity Relationship Diagram (Logical)

```
Category (1) ─────────────────────┬──────────── (M) Product
                                  └──────────── (M) Menu

Product (1) ─────────────── (M) ProductAllergen ─────────────── (1) Allergen
Product (M) ─────────────── (M) MenuProduct ─────────────── (1) Menu

User (1) ─────────────────────────── (M) Order
Order (1) ───────────────────────── (M) OrderItem ─────────────── (1) Product

Product (1) ───────────────────────── (M) ProductImage
Menu (1) ──────────────────────────── (M) MenuImage

Configuration - Singleton entity for application settings
```

## Entity Definitions

### 1. Category
```
Table: dbo.Category
Purpose: Group products and menus into logical categories
├─ CategoryId (PK)        INT NOT NULL IDENTITY(1,1)
├─ Name                   NVARCHAR(100) NOT NULL UNIQUE
├─ Description            NVARCHAR(500) NULL
├─ IsActive               BIT NOT NULL DEFAULT 1
└─ CreatedDate            DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- Name: Required, 1-100 characters, unique
- Description: Optional, max 500 characters
- IsActive: Soft-delete support (for historical orders)

Indexes:
- UNIQUE INDEX UX_Category_Name ON Category(Name)
- INDEX IX_Category_IsActive ON Category(IsActive)
```

### 2. Allergen
```
Table: dbo.Allergen
Purpose: Define types of allergens (gluten, eggs, lactose, etc.)
├─ AllergenId (PK)        INT NOT NULL IDENTITY(1,1)
├─ Name                   NVARCHAR(100) NOT NULL UNIQUE
└─ Description            NVARCHAR(500) NULL

Validation Rules:
- Name: Required, 1-100 characters, unique (e.g., "Gluten", "Eggs", "Peanuts")
- Description: Optional, max 500 characters

Indexes:
- UNIQUE INDEX UX_Allergen_Name ON Allergen(Name)
```

### 3. Product
```
Table: dbo.Product
Purpose: Individual dishes with prices, quantities, and allergen associations
├─ ProductId (PK)             INT NOT NULL IDENTITY(1,1)
├─ CategoryId (FK)            INT NOT NULL → Category.CategoryId
├─ Name                       NVARCHAR(150) NOT NULL
├─ Description                NVARCHAR(1000) NULL
├─ Price                      DECIMAL(10,2) NOT NULL
├─ PortionQuantity            INT NOT NULL (in grams)
├─ TotalQuantity              INT NOT NULL DEFAULT 0
├─ IsAvailable                BIT NOT NULL DEFAULT 1
├─ CreatedDate                DATETIME NOT NULL DEFAULT GETDATE()
├─ ModifiedDate               DATETIME NOT NULL DEFAULT GETDATE()
└─ IsDeleted                  BIT NOT NULL DEFAULT 0

Validation Rules:
- CategoryId: Required, must reference valid category
- Name: Required, 1-150 characters
- Price: Required, > 0, precision to 0.01 units
- PortionQuantity: Required, > 0, in grams (e.g., 300)
- TotalQuantity: >= 0, represents current restaurant inventory
- IsAvailable: Automatically set based on TotalQuantity > 0

Business Rules:
- Product marked unavailable when TotalQuantity = 0
- Price is per portion (serving)
- PortionQuantity is standard serving size displayed in menu

Indexes:
- INDEX IX_Product_CategoryId ON Product(CategoryId)
- INDEX IX_Product_IsAvailable ON Product(IsAvailable)
- INDEX IX_Product_Name ON Product(Name)
- UNIQUE INDEX UX_Product_Name_Category ON Product(Name, CategoryId) -- Name unique within category
```

### 4. ProductAllergen
```
Table: dbo.ProductAllergen
Purpose: Many-to-many association between products and allergens
├─ ProductAllergenId (PK)  INT NOT NULL IDENTITY(1,1)
├─ ProductId (FK)          INT NOT NULL → Product.ProductId
├─ AllergenId (FK)         INT NOT NULL → Allergen.AllergenId
└─ CONSTRAINT UC_ProductAllergen UNIQUE (ProductId, AllergenId)

Validation Rules:
- Cannot add same allergen to product twice
- Both foreign keys required

Indexes:
- INDEX IX_ProductAllergen_AllergenId ON ProductAllergen(AllergenId)
- INDEX IX_ProductAllergen_ProductId ON ProductAllergen(ProductId)
- UNIQUE INDEX UX_ProductAllergen ON ProductAllergen(ProductId, AllergenId)
```

### 5. ProductImage
```
Table: dbo.ProductImage
Purpose: Store multiple images per product for gallery display
├─ ProductImageId (PK)  INT NOT NULL IDENTITY(1,1)
├─ ProductId (FK)       INT NOT NULL → Product.ProductId
├─ ImageUrl             NVARCHAR(MAX) NOT NULL
├─ DisplayOrder         INT NOT NULL DEFAULT 1
└─ CreatedDate          DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- ImageUrl: Required, valid URI format
- DisplayOrder: >= 1, orders images in gallery (1 = first/main image)

Indexes:
- INDEX IX_ProductImage_ProductId ON ProductImage(ProductId)
- INDEX IX_ProductImage_DisplayOrder ON ProductImage(ProductId, DisplayOrder)
```

### 6. Menu (Bundle)
```
Table: dbo.Menu
Purpose: Bundle of products at special discounted price (e.g., platter, combo)
├─ MenuId (PK)                INT NOT NULL IDENTITY(1,1)
├─ CategoryId (FK)            INT NOT NULL → Category.CategoryId
├─ Name                       NVARCHAR(150) NOT NULL
├─ Description                NVARCHAR(1000) NULL
├─ BundleDiscountPercent      DECIMAL(5,2) NOT NULL (e.g., 15.50)
├─ IsAvailable                BIT NOT NULL DEFAULT 1
├─ CreatedDate                DATETIME NOT NULL DEFAULT GETDATE()
├─ ModifiedDate               DATETIME NOT NULL DEFAULT GETDATE()
└─ IsDeleted                  BIT NOT NULL DEFAULT 0

Validation Rules:
- CategoryId: Required, same structure as Product (shares categories)
- Name: Required, 1-150 characters
- BundleDiscountPercent: 0-100, applies to calculated total of constituent products
- Price: Calculated as SUM(constituent product prices) * (1 - BundleDiscountPercent/100)
- IsAvailable: Dependent on ALL constituent products being available

Business Rules:
- Bundle price not stored (calculated from products)
- Bundle unavailable if ANY constituent product is unavailable
- Discount percent read from configuration in some cases

Indexes:
- INDEX IX_Menu_CategoryId ON Menu(CategoryId)
- INDEX IX_Menu_IsAvailable ON Menu(IsAvailable)
```

### 7. MenuProduct
```
Table: dbo.MenuProduct
Purpose: Many-to-many association defining which products compose each menu/bundle
├─ MenuProductId (PK)   INT NOT NULL IDENTITY(1,1)
├─ MenuId (FK)          INT NOT NULL → Menu.MenuId
├─ ProductId (FK)       INT NOT NULL → Product.ProductId
└─ Quantity             INT NOT NULL (number of portions in menu)

Validation Rules:
- MenuId: Required, must reference valid menu
- ProductId: Required, must reference valid product
- Quantity: Required, > 0 (e.g., 1 for single product in combo, 2 for double portion)

Business Rules:
- Quantity represents number of portions (not grams)
- Bundle price = SUM(Product.Price * MenuProduct.Quantity) * (1 - Menu.BundleDiscountPercent/100)

Indexes:
- INDEX IX_MenuProduct_MenuId ON MenuProduct(MenuId)
- INDEX IX_MenuProduct_ProductId ON MenuProduct(ProductId)
- UNIQUE INDEX UX_MenuProduct ON MenuProduct(MenuId, ProductId)
```

### 8. MenuImage
```
Table: dbo.MenuImage
Purpose: Store multiple images per menu bundle for gallery display
├─ MenuImageId (PK)   INT NOT NULL IDENTITY(1,1)
├─ MenuId (FK)        INT NOT NULL → Menu.MenuId
├─ ImageUrl           NVARCHAR(MAX) NOT NULL
├─ DisplayOrder       INT NOT NULL DEFAULT 1
└─ CreatedDate        DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- Same as ProductImage

Indexes:
- INDEX IX_MenuImage_MenuId ON MenuImage(MenuId)
```

### 9. User
```
Table: dbo.User
Purpose: Application users (clients and employees)
├─ UserId (PK)             INT NOT NULL IDENTITY(1,1)
├─ Email                   NVARCHAR(255) NOT NULL UNIQUE
├─ PasswordHash            NVARCHAR(255) NOT NULL (bcrypt hash)
├─ FirstName               NVARCHAR(100) NOT NULL
├─ LastName                NVARCHAR(100) NOT NULL
├─ PhoneNumber             NVARCHAR(20) NULL
├─ DeliveryAddress         NVARCHAR(500) NULL
├─ Role                    NVARCHAR(20) NOT NULL (enum: 'Client', 'Employee')
├─ IsActive                BIT NOT NULL DEFAULT 1
├─ CreatedDate             DATETIME NOT NULL DEFAULT GETDATE()
├─ ModifiedDate            DATETIME NOT NULL DEFAULT GETDATE()
└─ LastLoginDate           DATETIME NULL

Validation Rules:
- Email: Required, unique, valid email format, max 255 chars
- PasswordHash: Required, bcrypt hash (never plaintext)
- FirstName: Required, 1-100 characters
- LastName: Required, 1-100 characters
- PhoneNumber: Optional, max 20 characters
- DeliveryAddress: Optional, max 500 characters (required for orders)
- Role: Required, one of: 'Client' or 'Employee'

Business Rules:
- Email is primary login identifier
- No plaintext passwords ever stored
- DeliveryAddress required before placing order
- IsActive enables soft-delete (for historical data)

Indexes:
- UNIQUE INDEX UX_User_Email ON User(Email)
- INDEX IX_User_Role ON User(Role)
- INDEX IX_User_IsActive ON User(IsActive)
```

### 10. Order
```
Table: dbo.Order
Purpose: Customer order with automatic status tracking and financial details
├─ OrderId (PK)               INT NOT NULL IDENTITY(1,1)
├─ UserId (FK)                INT NOT NULL → User.UserId
├─ OrderCode                  NVARCHAR(20) NOT NULL UNIQUE (customer-facing identifier)
├─ OrderDate                  DATETIME NOT NULL DEFAULT GETDATE()
├─ Status                     NVARCHAR(20) NOT NULL DEFAULT 'inregistrata'
├─ SubTotal                   DECIMAL(10,2) NOT NULL (sum of items)
├─ ShippingFee                DECIMAL(10,2) NOT NULL DEFAULT 0
├─ DiscountAmount             DECIMAL(10,2) NOT NULL DEFAULT 0
├─ TotalCost                  DECIMAL(10,2) NOT NULL (SubTotal + ShippingFee - DiscountAmount)
├─ EstimatedDeliveryTime      DATETIME NULL
├─ ActualDeliveryTime         DATETIME NULL
├─ DeliveryAddress            NVARCHAR(500) NULL
├─ Notes                      NVARCHAR(1000) NULL
└─ ModifiedDate               DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- UserId: Required, must reference valid user
- OrderCode: Required, unique, auto-generated (e.g., "ORD-20260505-001")
- Status: One of: 'inregistrata', 'se pregateste', 'a plecat la client', 'livrata', 'anulata'
- SubTotal: > 0, sum of order items
- ShippingFee: >= 0, calculated based on subtotal thresholds
- DiscountAmount: >= 0, calculated based on business rules
- TotalCost: = SubTotal + ShippingFee - DiscountAmount

Business Rules:
- Status transitions: inregistrata → se pregateste → a plecat la client → livrata (or anulata anytime)
- Cannot update status of cancelled or delivered orders
- OrderCode displayed to customer (not OrderId)
- EstimatedDeliveryTime calculated at order creation (CurrentTime + X minutes)
- DeliveryAddress: defaults to user's delivery address but can be overridden per order

Indexes:
- INDEX IX_Order_UserId ON Order(UserId)
- UNIQUE INDEX UX_Order_OrderCode ON Order(OrderCode)
- INDEX IX_Order_Status ON Order(Status)
- INDEX IX_Order_OrderDate ON Order(OrderDate)
```

### 11. OrderItem
```
Table: dbo.OrderItem
Purpose: Line items in an order (product + quantity + price at time of order)
├─ OrderItemId (PK)      INT NOT NULL IDENTITY(1,1)
├─ OrderId (FK)          INT NOT NULL → Order.OrderId
├─ ProductId (FK)        INT NOT NULL → Product.ProductId
├─ Quantity              INT NOT NULL (number of portions ordered)
├─ UnitPrice             DECIMAL(10,2) NOT NULL (price per portion at order time)
├─ ItemTotal             DECIMAL(10,2) NOT NULL (Quantity * UnitPrice)
└─ CreatedDate           DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- OrderId: Required
- ProductId: Required, must reference valid product
- Quantity: Required, >= 1
- UnitPrice: Required, >= 0 (price at order creation time, not current product price)
- ItemTotal: = Quantity * UnitPrice

Business Rules:
- UnitPrice stored separately to maintain order history (product price may change later)
- One order can have multiple items for same product (e.g., 2x Soup in same order)

Indexes:
- INDEX IX_OrderItem_OrderId ON OrderItem(OrderId)
- INDEX IX_OrderItem_ProductId ON OrderItem(ProductId)
```

### 12. Configuration
```
Table: dbo.Configuration
Purpose: Store application configuration parameters (can also use external XML file)
├─ ConfigId (PK)                       INT NOT NULL IDENTITY(1,1)
├─ MinOrderForFreeShipping             DECIMAL(10,2) NOT NULL (e.g., 100)
├─ ShippingFee                         DECIMAL(10,2) NOT NULL (e.g., 15)
├─ LargeOrderDiscountThreshold         DECIMAL(10,2) NOT NULL (e.g., 200)
├─ LargeOrderDiscountPercent           DECIMAL(5,2) NOT NULL (e.g., 10)
├─ MenuBundleDiscountPercent           DECIMAL(5,2) NOT NULL (e.g., 15)
├─ FrequentOrderThreshold              INT NOT NULL (e.g., 5 orders)
├─ FrequentOrderTimeWindow             INT NOT NULL (in days, e.g., 30)
├─ FrequentOrderDiscountPercent        DECIMAL(5,2) NOT NULL (e.g., 8)
├─ LowStockThreshold                   INT NOT NULL (in grams, e.g., 500)
└─ ModifiedDate                        DATETIME NOT NULL DEFAULT GETDATE()

Validation Rules:
- All monetary values: >= 0
- All percentages: 0-100
- All thresholds: >= 1

Business Rules:
- Only one row in this table (ConfigId = 1)
- Alternative: Read from external RestaurantConfig.xml file
- Service layer caches configuration in memory on application startup
```

## Normalization Verification (3NF)

### First Normal Form (1NF): Atomic Values
✓ All columns contain atomic values
✓ No repeating groups (images in separate table, allergens in junction table)

### Second Normal Form (2NF): No Partial Dependencies
✓ All non-key attributes depend on entire primary key
✓ Single-column primary keys, so no partial dependency issues

### Third Normal Form (3NF): No Transitive Dependencies
✓ Product → Category → CategoryId: Direct dependency only
✓ Order → User → UserId: Direct dependency only
✓ OrderItem depends only on OrderId and ProductId, not on Order's attributes
✓ No non-key attribute depends on another non-key attribute

### Example: Product(Name) does NOT depend on Category(Name)
- Product.Name is independent of Category.Name
- They are in different entities with proper foreign key relationship
- No transitive dependency

## Summary

This schema provides:
- **Data Integrity**: Foreign keys enforce referential integrity
- **Query Performance**: Proper indexes on frequently queried columns
- **Business Logic Support**: All features from specification supported
- **Scalability**: Normalized structure enables easy data growth
- **Maintainability**: Clear entity responsibilities and relationships
- **Compliance**: Meets 3NF requirement and supports 10+ stored procedures
