-- =====================================================
-- Database Schema Creation Script
-- Restaurant Order Management System
-- =====================================================
-- Purpose: Create all tables in 3NF with proper indexes and constraints
-- Date: May 5, 2026
-- Target: SQL Server 2019+
-- =====================================================

USE RestaurantOrderManagement;
GO

-- =====================================================
-- 1. CATEGORY TABLE
-- =====================================================
IF OBJECT_ID('dbo.Category', 'U') IS NOT NULL
    DROP TABLE dbo.Category;
GO

CREATE TABLE dbo.Category (
    CategoryId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL UNIQUE,
    [Description] NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE UNIQUE INDEX UX_Category_Name ON dbo.Category([Name]);
CREATE INDEX IX_Category_IsActive ON dbo.Category(IsActive);
GO

-- =====================================================
-- 2. ALLERGEN TABLE
-- =====================================================
IF OBJECT_ID('dbo.Allergen', 'U') IS NOT NULL
    DROP TABLE dbo.Allergen;
GO

CREATE TABLE dbo.Allergen (
    AllergenId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL UNIQUE,
    [Description] NVARCHAR(500) NULL
);

CREATE UNIQUE INDEX UX_Allergen_Name ON dbo.Allergen([Name]);
GO

-- =====================================================
-- 3. PRODUCT TABLE
-- =====================================================
IF OBJECT_ID('dbo.Product', 'U') IS NOT NULL
    DROP TABLE dbo.Product;
GO

CREATE TABLE dbo.Product (
    ProductId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    Price DECIMAL(10,2) NOT NULL,
    PortionQuantity INT NOT NULL,
    TotalQuantity INT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId)
);

CREATE INDEX IX_Product_CategoryId ON dbo.Product(CategoryId);
CREATE INDEX IX_Product_IsAvailable ON dbo.Product(IsAvailable);
CREATE INDEX IX_Product_Name ON dbo.Product([Name]);
CREATE UNIQUE INDEX UX_Product_Name_Category ON dbo.Product([Name], CategoryId);
GO

-- =====================================================
-- 4. PRODUCT_ALLERGEN TABLE (M2M Junction)
-- =====================================================
IF OBJECT_ID('dbo.ProductAllergen', 'U') IS NOT NULL
    DROP TABLE dbo.ProductAllergen;
GO

CREATE TABLE dbo.ProductAllergen (
    ProductAllergenId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    AllergenId INT NOT NULL,
    CONSTRAINT FK_ProductAllergen_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_ProductAllergen_Allergen FOREIGN KEY (AllergenId) REFERENCES dbo.Allergen(AllergenId),
    CONSTRAINT UC_ProductAllergen UNIQUE (ProductId, AllergenId)
);

CREATE INDEX IX_ProductAllergen_ProductId ON dbo.ProductAllergen(ProductId);
CREATE INDEX IX_ProductAllergen_AllergenId ON dbo.ProductAllergen(AllergenId);
GO

-- =====================================================
-- 5. PRODUCT_IMAGE TABLE
-- =====================================================
IF OBJECT_ID('dbo.ProductImage', 'U') IS NOT NULL
    DROP TABLE dbo.ProductImage;
GO

CREATE TABLE dbo.ProductImage (
    ProductImageId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ProductImage_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId) ON DELETE CASCADE
);

CREATE INDEX IX_ProductImage_ProductId ON dbo.ProductImage(ProductId);
CREATE INDEX IX_ProductImage_DisplayOrder ON dbo.ProductImage(ProductId, DisplayOrder);
GO

-- =====================================================
-- 6. MENU TABLE (Bundle)
-- =====================================================
IF OBJECT_ID('dbo.Menu', 'U') IS NOT NULL
    DROP TABLE dbo.Menu;
GO

CREATE TABLE dbo.Menu (
    MenuId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    BundleDiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Menu_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId)
);

CREATE INDEX IX_Menu_CategoryId ON dbo.Menu(CategoryId);
CREATE INDEX IX_Menu_IsAvailable ON dbo.Menu(IsAvailable);
GO

-- =====================================================
-- 7. MENU_PRODUCT TABLE (M2M Junction)
-- =====================================================
IF OBJECT_ID('dbo.MenuProduct', 'U') IS NOT NULL
    DROP TABLE dbo.MenuProduct;
GO

CREATE TABLE dbo.MenuProduct (
    MenuProductId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    MenuId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CONSTRAINT FK_MenuProduct_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menu(MenuId) ON DELETE CASCADE,
    CONSTRAINT FK_MenuProduct_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT UC_MenuProduct UNIQUE (MenuId, ProductId)
);

CREATE INDEX IX_MenuProduct_MenuId ON dbo.MenuProduct(MenuId);
CREATE INDEX IX_MenuProduct_ProductId ON dbo.MenuProduct(ProductId);
GO

-- =====================================================
-- 8. MENU_IMAGE TABLE
-- =====================================================
IF OBJECT_ID('dbo.MenuImage', 'U') IS NOT NULL
    DROP TABLE dbo.MenuImage;
GO

CREATE TABLE dbo.MenuImage (
    MenuImageId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    MenuId INT NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_MenuImage_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menu(MenuId) ON DELETE CASCADE
);

CREATE INDEX IX_MenuImage_MenuId ON dbo.MenuImage(MenuId);
GO

-- =====================================================
-- 9. USER TABLE
-- =====================================================
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL
    DROP TABLE dbo.[User];
GO

CREATE TABLE dbo.[User] (
    UserId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    DeliveryAddress NVARCHAR(500) NULL,
    [Role] NVARCHAR(20) NOT NULL DEFAULT 'Client',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL
);

CREATE UNIQUE INDEX UX_User_Email ON dbo.[User](Email);
CREATE INDEX IX_User_Role ON dbo.[User]([Role]);
CREATE INDEX IX_User_IsActive ON dbo.[User](IsActive);
GO

-- =====================================================
-- 10. ORDER TABLE
-- =====================================================
IF OBJECT_ID('dbo.[Order]', 'U') IS NOT NULL
    DROP TABLE dbo.[Order];
GO

CREATE TABLE dbo.[Order] (
    OrderId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderCode NVARCHAR(20) NOT NULL UNIQUE,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'inregistrata',
    SubTotal DECIMAL(10,2) NOT NULL,
    ShippingFee DECIMAL(10,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalCost DECIMAL(10,2) NOT NULL,
    EstimatedDeliveryTime DATETIME NULL,
    ActualDeliveryTime DATETIME NULL,
    DeliveryAddress NVARCHAR(500) NULL,
    Notes NVARCHAR(1000) NULL,
    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Order_User FOREIGN KEY (UserId) REFERENCES dbo.[User](UserId)
);

CREATE INDEX IX_Order_UserId ON dbo.[Order](UserId);
CREATE UNIQUE INDEX UX_Order_OrderCode ON dbo.[Order](OrderCode);
CREATE INDEX IX_Order_Status ON dbo.[Order]([Status]);
CREATE INDEX IX_Order_OrderDate ON dbo.[Order](OrderDate);
GO

-- =====================================================
-- 11. ORDER_ITEM TABLE
-- =====================================================
IF OBJECT_ID('dbo.OrderItem', 'U') IS NOT NULL
    DROP TABLE dbo.OrderItem;
GO

CREATE TABLE dbo.OrderItem (
    OrderItemId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NULL,
    MenuId INT NULL,
    ItemType NVARCHAR(20) NOT NULL DEFAULT 'Preparat',
    ItemName NVARCHAR(150) NOT NULL DEFAULT '',
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    ItemTotal DECIMAL(10,2) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (OrderId) REFERENCES dbo.[Order](OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_OrderItem_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menu(MenuId),
    CONSTRAINT CK_OrderItem_Target CHECK (
        (ProductId IS NOT NULL AND MenuId IS NULL AND ItemType = 'Preparat')
        OR
        (ProductId IS NULL AND MenuId IS NOT NULL AND ItemType = 'Meniu')
    )
);

CREATE INDEX IX_OrderItem_OrderId ON dbo.OrderItem(OrderId);
CREATE INDEX IX_OrderItem_ProductId ON dbo.OrderItem(ProductId);
CREATE INDEX IX_OrderItem_MenuId ON dbo.OrderItem(MenuId);
GO

-- =====================================================
-- 12. CONFIGURATION TABLE
-- =====================================================
IF OBJECT_ID('dbo.Configuration', 'U') IS NOT NULL
    DROP TABLE dbo.Configuration;
GO

CREATE TABLE dbo.Configuration (
    ConfigId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    MinOrderForFreeShipping DECIMAL(10,2) NOT NULL DEFAULT 100,
    ShippingFee DECIMAL(10,2) NOT NULL DEFAULT 15,
    LargeOrderDiscountThreshold DECIMAL(10,2) NOT NULL DEFAULT 200,
    LargeOrderDiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 10,
    MenuBundleDiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 15,
    FrequentOrderThreshold INT NOT NULL DEFAULT 5,
    FrequentOrderTimeWindow INT NOT NULL DEFAULT 30,
    FrequentOrderDiscountPercent DECIMAL(5,2) NOT NULL DEFAULT 8,
    LowStockThreshold INT NOT NULL DEFAULT 500,
    ModifiedDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Insert default configuration
INSERT INTO dbo.Configuration DEFAULT VALUES;
GO

PRINT 'Database schema created successfully.';
