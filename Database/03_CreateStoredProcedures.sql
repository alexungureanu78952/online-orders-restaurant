-- =====================================================
-- Stored Procedures for Restaurant Order Management
-- =====================================================
-- Purpose: All data operations through parameterized stored procedures
-- Date: May 5, 2026
-- Target: SQL Server 2019+
-- =====================================================

USE RestaurantOrderManagement;
GO

-- =====================================================
-- CATEGORY PROCEDURES
-- =====================================================

-- sp_GetCategories: Retrieve all active categories
IF OBJECT_ID('dbo.sp_GetCategories', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCategories;
GO

CREATE PROCEDURE dbo.sp_GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CategoryId,
        [Name],
        [Description],
        IsActive,
        CreatedDate
    FROM dbo.Category
    WHERE IsActive = 1
    ORDER BY [Name];
END;
GO

-- =====================================================
-- PRODUCT PROCEDURES
-- =====================================================

-- sp_GetProductById: Retrieve product by ID with allergens
IF OBJECT_ID('dbo.sp_GetProductById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductById;
GO

CREATE PROCEDURE dbo.sp_GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.ProductId,
        p.CategoryId,
        p.[Name],
        p.[Description],
        p.Price,
        p.PortionQuantity,
        p.TotalQuantity,
        p.IsAvailable,
        p.CreatedDate,
        p.ModifiedDate,
        c.[Name] AS CategoryName
    FROM dbo.Product p
    INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    WHERE p.ProductId = @ProductId
        AND p.IsDeleted = 0;
    
    -- Get allergens for product
    SELECT 
        a.AllergenId,
        a.[Name],
        a.[Description]
    FROM dbo.Allergen a
    INNER JOIN dbo.ProductAllergen pa ON a.AllergenId = pa.AllergenId
    WHERE pa.ProductId = @ProductId;
END;
GO

-- sp_GetProductsByCategory: Retrieve products by category ID
IF OBJECT_ID('dbo.sp_GetProductsByCategory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetProductsByCategory;
GO

CREATE PROCEDURE dbo.sp_GetProductsByCategory
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.ProductId,
        p.CategoryId,
        p.[Name],
        p.[Description],
        p.Price,
        p.PortionQuantity,
        p.TotalQuantity,
        p.IsAvailable,
        p.CreatedDate,
        p.ModifiedDate,
        c.[Name] AS CategoryName
    FROM dbo.Product p
    INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    WHERE p.CategoryId = @CategoryId
        AND p.IsDeleted = 0
        AND p.IsAvailable = 1
    ORDER BY p.[Name];
END;
GO

-- sp_SearchProducts: Search products by keyword with allergen filters
IF OBJECT_ID('dbo.sp_SearchProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SearchProducts;
GO

CREATE PROCEDURE dbo.sp_SearchProducts
    @Keyword NVARCHAR(150) = NULL,
    @IncludeAllergens NVARCHAR(MAX) = NULL,
    @ExcludeAllergens NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SearchPattern NVARCHAR(155);
    IF @Keyword IS NOT NULL
        SET @SearchPattern = '%' + @Keyword + '%';
    
    -- Create temp tables for allergen parsing
    DECLARE @IncludeAllergenIds TABLE (AllergenId INT);
    DECLARE @ExcludeAllergenIds TABLE (AllergenId INT);
    
    -- Parse include allergens (comma-separated)
    IF @IncludeAllergens IS NOT NULL
    BEGIN
        INSERT INTO @IncludeAllergenIds
        SELECT CAST(value AS INT) FROM STRING_SPLIT(@IncludeAllergens, ',');
    END;
    
    -- Parse exclude allergens
    IF @ExcludeAllergens IS NOT NULL
    BEGIN
        INSERT INTO @ExcludeAllergenIds
        SELECT CAST(value AS INT) FROM STRING_SPLIT(@ExcludeAllergens, ',');
    END;
    
    -- Main query
    SELECT DISTINCT
        p.ProductId,
        p.CategoryId,
        p.[Name],
        p.[Description],
        p.Price,
        p.PortionQuantity,
        p.TotalQuantity,
        p.IsAvailable,
        c.[Name] AS CategoryName
    FROM dbo.Product p
    INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    LEFT JOIN dbo.ProductAllergen pa ON p.ProductId = pa.ProductId
    WHERE p.IsDeleted = 0
        AND p.IsAvailable = 1
        AND (@SearchPattern IS NULL OR p.[Name] LIKE @SearchPattern OR p.[Description] LIKE @SearchPattern)
        AND (@IncludeAllergens IS NULL OR NOT EXISTS (
            SELECT 1 FROM @IncludeAllergenIds ial
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.ProductAllergen pa2
                WHERE pa2.ProductId = p.ProductId AND pa2.AllergenId = ial.AllergenId
            )
        ))
        AND NOT EXISTS (
            SELECT 1 FROM @ExcludeAllergenIds eal
            WHERE EXISTS (
                SELECT 1 FROM dbo.ProductAllergen pa3
                WHERE pa3.ProductId = p.ProductId AND pa3.AllergenId = eal.AllergenId
            )
        )
    ORDER BY c.[Name], p.[Name];
END;
GO

-- sp_CreateProduct: Insert new product
IF OBJECT_ID('dbo.sp_CreateProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateProduct;
GO

CREATE PROCEDURE dbo.sp_CreateProduct
    @CategoryId INT,
    @Name NVARCHAR(150),
    @Description NVARCHAR(1000) = NULL,
    @Price DECIMAL(10,2),
    @PortionQuantity INT,
    @TotalQuantity INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.Product (CategoryId, [Name], [Description], Price, PortionQuantity, TotalQuantity, IsAvailable)
    VALUES (@CategoryId, @Name, @Description, @Price, @PortionQuantity, @TotalQuantity, CASE WHEN @TotalQuantity > 0 THEN 1 ELSE 0 END);
    
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ProductId;
END;
GO

-- sp_UpdateProduct: Update existing product
IF OBJECT_ID('dbo.sp_UpdateProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateProduct;
GO

CREATE PROCEDURE dbo.sp_UpdateProduct
    @ProductId INT,
    @Name NVARCHAR(150),
    @Description NVARCHAR(1000) = NULL,
    @Price DECIMAL(10,2),
    @PortionQuantity INT,
    @IsAvailable BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Product
    SET [Name] = @Name,
        [Description] = @Description,
        Price = @Price,
        PortionQuantity = @PortionQuantity,
        IsAvailable = @IsAvailable,
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId
        AND IsDeleted = 0;
END;
GO

-- sp_DeleteProduct: Soft delete product
IF OBJECT_ID('dbo.sp_DeleteProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteProduct;
GO

CREATE PROCEDURE dbo.sp_DeleteProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Product
    SET IsDeleted = 1,
        IsAvailable = 0,
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId;
END;
GO

-- =====================================================
-- USER PROCEDURES
-- =====================================================

-- sp_CreateUser: Register new user
IF OBJECT_ID('dbo.sp_CreateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateUser;
GO

CREATE PROCEDURE dbo.sp_CreateUser
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @PhoneNumber NVARCHAR(20) = NULL,
    @DeliveryAddress NVARCHAR(500) = NULL,
    @Role NVARCHAR(20) = 'Client'
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        INSERT INTO dbo.[User] (Email, PasswordHash, FirstName, LastName, PhoneNumber, DeliveryAddress, [Role])
        VALUES (@Email, @PasswordHash, @FirstName, @LastName, @PhoneNumber, @DeliveryAddress, @Role);
        
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
    END TRY
    BEGIN CATCH
        -- Email uniqueness constraint violated
        IF ERROR_NUMBER() = 2627
            THROW 50001, 'Email already exists', 1;
        ELSE
            THROW;
    END CATCH;
END;
GO

-- sp_GetUserByEmail: Retrieve user for authentication
IF OBJECT_ID('dbo.sp_GetUserByEmail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserByEmail;
GO

CREATE PROCEDURE dbo.sp_GetUserByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        UserId,
        Email,
        PasswordHash,
        FirstName,
        LastName,
        PhoneNumber,
        DeliveryAddress,
        [Role],
        IsActive,
        LastLoginDate
    FROM dbo.[User]
    WHERE Email = @Email
        AND IsActive = 1;
END;
GO

-- sp_UpdateLastLoginDate: Update last login timestamp
IF OBJECT_ID('dbo.sp_UpdateLastLoginDate', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateLastLoginDate;
GO

CREATE PROCEDURE dbo.sp_UpdateLastLoginDate
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.[User]
    SET LastLoginDate = GETDATE()
    WHERE UserId = @UserId;
END;
GO

-- =====================================================
-- ORDER PROCEDURES
-- =====================================================

-- sp_CreateOrder: Create new order with items
IF OBJECT_ID('dbo.sp_CreateOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateOrder;
GO

CREATE PROCEDURE dbo.sp_CreateOrder
    @UserId INT,
    @SubTotal DECIMAL(10,2),
    @ShippingFee DECIMAL(10,2) = 0,
    @DiscountAmount DECIMAL(10,2) = 0,
    @DeliveryAddress NVARCHAR(500) = NULL,
    @Notes NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OrderCode NVARCHAR(20);
    DECLARE @TotalCost DECIMAL(10,2);
    DECLARE @EstimatedDeliveryTime DATETIME;
    
    -- Generate unique order code: ORD-YYYYMMDD-XXXXX
    SET @OrderCode = 'ORD-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + REPLICATE('0', 5 - LEN(CAST(IDENT_CURRENT('dbo.[Order]') + 1 AS VARCHAR))) + CAST(IDENT_CURRENT('dbo.[Order]') + 1 AS VARCHAR);
    
    -- Calculate total
    SET @TotalCost = @SubTotal + @ShippingFee - @DiscountAmount;
    
    -- Estimate delivery (30 minutes from now)
    SET @EstimatedDeliveryTime = DATEADD(MINUTE, 30, GETDATE());
    
    INSERT INTO dbo.[Order] (UserId, OrderCode, [Status], SubTotal, ShippingFee, DiscountAmount, TotalCost, EstimatedDeliveryTime, DeliveryAddress, Notes)
    VALUES (@UserId, @OrderCode, 'inregistrata', @SubTotal, @ShippingFee, @DiscountAmount, @TotalCost, @EstimatedDeliveryTime, @DeliveryAddress, @Notes);
    
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS OrderId, @OrderCode AS OrderCode;
END;
GO

-- sp_GetOrderDetails: Retrieve order with line items
IF OBJECT_ID('dbo.sp_GetOrderDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOrderDetails;
GO

CREATE PROCEDURE dbo.sp_GetOrderDetails
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Order header
    SELECT 
        o.OrderId,
        o.UserId,
        o.OrderCode,
        o.OrderDate,
        o.[Status],
        o.SubTotal,
        o.ShippingFee,
        o.DiscountAmount,
        o.TotalCost,
        o.EstimatedDeliveryTime,
        o.ActualDeliveryTime,
        o.DeliveryAddress,
        o.Notes,
        u.FirstName,
        u.LastName,
        u.Email
    FROM dbo.[Order] o
    INNER JOIN dbo.[User] u ON o.UserId = u.UserId
    WHERE o.OrderId = @OrderId;
    
    -- Order items
    SELECT 
        oi.OrderItemId,
        oi.ProductId,
        oi.Quantity,
        oi.UnitPrice,
        oi.ItemTotal,
        p.[Name] AS ProductName
    FROM dbo.OrderItem oi
    INNER JOIN dbo.Product p ON oi.ProductId = p.ProductId
    WHERE oi.OrderId = @OrderId
    ORDER BY oi.OrderItemId;
END;
GO

-- sp_GetUserOrders: Retrieve all orders for user
IF OBJECT_ID('dbo.sp_GetUserOrders', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserOrders;
GO

CREATE PROCEDURE dbo.sp_GetUserOrders
    @UserId INT,
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Limit)
        o.OrderId,
        o.OrderCode,
        o.OrderDate,
        o.[Status],
        o.SubTotal,
        o.ShippingFee,
        o.DiscountAmount,
        o.TotalCost,
        o.EstimatedDeliveryTime,
        o.ActualDeliveryTime
    FROM dbo.[Order] o
    WHERE o.UserId = @UserId
    ORDER BY o.OrderDate DESC;
END;
GO

-- sp_GetAllOrders: Retrieve all orders (admin only)
IF OBJECT_ID('dbo.sp_GetAllOrders', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAllOrders;
GO

CREATE PROCEDURE dbo.sp_GetAllOrders
    @Status NVARCHAR(20) = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Limit)
        o.OrderId,
        o.OrderCode,
        o.OrderDate,
        o.[Status],
        o.SubTotal,
        o.ShippingFee,
        o.DiscountAmount,
        o.TotalCost,
        u.FirstName,
        u.LastName,
        u.Email
    FROM dbo.[Order] o
    INNER JOIN dbo.[User] u ON o.UserId = u.UserId
    WHERE (@Status IS NULL OR o.[Status] = @Status)
        AND (@FromDate IS NULL OR o.OrderDate >= @FromDate)
        AND (@ToDate IS NULL OR o.OrderDate <= @ToDate)
    ORDER BY o.OrderDate DESC;
END;
GO

-- sp_UpdateOrderStatus: Update order status
IF OBJECT_ID('dbo.sp_UpdateOrderStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateOrderStatus;
GO

CREATE PROCEDURE dbo.sp_UpdateOrderStatus
    @OrderId INT,
    @NewStatus NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentStatus NVARCHAR(20);
    SELECT @CurrentStatus = [Status] FROM dbo.[Order] WHERE OrderId = @OrderId;
    
    -- Prevent status transitions from completed/cancelled orders
    IF @CurrentStatus IN ('livrata', 'anulata')
        THROW 50002, 'Cannot update status of completed or cancelled order', 1;
    
    -- Set actual delivery time when marking as delivered
    UPDATE dbo.[Order]
    SET [Status] = @NewStatus,
        ActualDeliveryTime = CASE WHEN @NewStatus = 'livrata' THEN GETDATE() ELSE ActualDeliveryTime END,
        ModifiedDate = GETDATE()
    WHERE OrderId = @OrderId;
END;
GO

-- =====================================================
-- INVENTORY PROCEDURES
-- =====================================================

-- sp_UpdateInventory: Update product stock after order
IF OBJECT_ID('dbo.sp_UpdateInventory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateInventory;
GO

CREATE PROCEDURE dbo.sp_UpdateInventory
    @ProductId INT,
    @QuantityChange INT  -- Negative for decrease, positive for restock
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Product
    SET TotalQuantity = TotalQuantity + @QuantityChange,
        IsAvailable = CASE WHEN TotalQuantity + @QuantityChange > 0 THEN 1 ELSE 0 END,
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId;
END;
GO

-- sp_GetLowStockProducts: Retrieve products below stock threshold
IF OBJECT_ID('dbo.sp_GetLowStockProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLowStockProducts;
GO

CREATE PROCEDURE dbo.sp_GetLowStockProducts
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @LowStockThreshold INT;
    SELECT @LowStockThreshold = LowStockThreshold FROM dbo.Configuration WHERE ConfigId = 1;
    
    SELECT 
        p.ProductId,
        p.[Name],
        p.TotalQuantity,
        c.[Name] AS CategoryName,
        @LowStockThreshold AS Threshold
    FROM dbo.Product p
    INNER JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    WHERE p.TotalQuantity < @LowStockThreshold
        AND p.IsDeleted = 0
    ORDER BY p.TotalQuantity ASC;
END;
GO

-- =====================================================
-- CONFIGURATION PROCEDURES
-- =====================================================

-- sp_GetConfiguration: Retrieve all configuration settings
IF OBJECT_ID('dbo.sp_GetConfiguration', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetConfiguration;
GO

CREATE PROCEDURE dbo.sp_GetConfiguration
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ConfigId,
        MinOrderForFreeShipping,
        ShippingFee,
        LargeOrderDiscountThreshold,
        LargeOrderDiscountPercent,
        MenuBundleDiscountPercent,
        FrequentOrderThreshold,
        FrequentOrderTimeWindow,
        FrequentOrderDiscountPercent,
        LowStockThreshold
    FROM dbo.Configuration
    WHERE ConfigId = 1;
END;
GO

-- sp_UpdateConfiguration: Update configuration settings
IF OBJECT_ID('dbo.sp_UpdateConfiguration', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateConfiguration;
GO

CREATE PROCEDURE dbo.sp_UpdateConfiguration
    @MinOrderForFreeShipping DECIMAL(10,2) = NULL,
    @ShippingFee DECIMAL(10,2) = NULL,
    @LargeOrderDiscountThreshold DECIMAL(10,2) = NULL,
    @LargeOrderDiscountPercent DECIMAL(5,2) = NULL,
    @LowStockThreshold INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Configuration
    SET MinOrderForFreeShipping = ISNULL(@MinOrderForFreeShipping, MinOrderForFreeShipping),
        ShippingFee = ISNULL(@ShippingFee, ShippingFee),
        LargeOrderDiscountThreshold = ISNULL(@LargeOrderDiscountThreshold, LargeOrderDiscountThreshold),
        LargeOrderDiscountPercent = ISNULL(@LargeOrderDiscountPercent, LargeOrderDiscountPercent),
        LowStockThreshold = ISNULL(@LowStockThreshold, LowStockThreshold),
        ModifiedDate = GETDATE()
    WHERE ConfigId = 1;
END;
GO

PRINT 'Stored procedures created successfully.';
