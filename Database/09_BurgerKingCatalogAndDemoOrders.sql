-- =====================================================
-- Keep the demo catalog focused on Burger King samples
-- and add realistic demo orders for reports/order testing.
-- =====================================================

USE RestaurantOrderManagement;
GO

SET NOCOUNT ON;

DECLARE @BurgerKingCategories TABLE ([Name] NVARCHAR(100) PRIMARY KEY);
INSERT INTO @BurgerKingCategories ([Name])
VALUES
    ('Burgeri'),
    ('Pui'),
    ('Garnituri'),
    ('Bauturi BK'),
    ('Deserturi BK'),
    ('Meniuri BK');

DECLARE @BurgerKingProductCategories TABLE ([Name] NVARCHAR(100) PRIMARY KEY);
INSERT INTO @BurgerKingProductCategories ([Name])
VALUES
    ('Burgeri'),
    ('Pui'),
    ('Garnituri'),
    ('Bauturi BK'),
    ('Deserturi BK');

-- Hide the older generic seed categories/products/menus so the UI only shows BK demo data.
UPDATE c
SET IsActive = CASE WHEN bk.[Name] IS NULL THEN 0 ELSE 1 END
FROM dbo.Category c
LEFT JOIN @BurgerKingCategories bk ON bk.[Name] = c.[Name];

UPDATE p
SET IsDeleted = CASE WHEN bk.[Name] IS NULL THEN 1 ELSE 0 END,
    IsAvailable = CASE WHEN bk.[Name] IS NULL THEN 0 ELSE CASE WHEN p.TotalQuantity > 0 THEN 1 ELSE 0 END END,
    ModifiedDate = GETDATE()
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
LEFT JOIN @BurgerKingProductCategories bk ON bk.[Name] = c.[Name];

UPDATE m
SET IsDeleted = CASE WHEN c.[Name] = 'Meniuri BK' THEN 0 ELSE 1 END,
    IsAvailable = CASE WHEN c.[Name] = 'Meniuri BK' THEN 1 ELSE 0 END,
    ModifiedDate = GETDATE()
FROM dbo.Menu m
INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId;

-- Keep a couple of BK items below the default low-stock threshold so Inventory can be tested.
UPDATE p
SET TotalQuantity = seed.TotalQuantity,
    IsAvailable = 1,
    IsDeleted = 0,
    ModifiedDate = GETDATE()
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
INNER JOIN (VALUES
    ('Garnituri', '6 buc. Inele de ceapa', 240),
    ('Deserturi BK', 'King Sundae Ciocolata', 180)
) AS seed (CategoryName, ProductName, TotalQuantity)
    ON seed.CategoryName = c.[Name] AND seed.ProductName = p.[Name];

-- Make sure the client used in local testing exists.
MERGE dbo.[User] AS target
USING (VALUES
    ('alex@u.com',
     '7Typ4rr8paxBMI1pxbpYRRo6a+/SAPXf1jL7G/wOZMw=',
     'Alex',
     'Ungureanu',
     '0759457942',
     'str. memorandului 32',
     'Client')
) AS source (Email, PasswordHash, FirstName, LastName, PhoneNumber, DeliveryAddress, [Role])
ON target.Email = source.Email
WHEN MATCHED THEN
    UPDATE SET FirstName = COALESCE(NULLIF(target.FirstName, ''), source.FirstName),
               LastName = COALESCE(NULLIF(target.LastName, ''), source.LastName),
               PhoneNumber = COALESCE(NULLIF(target.PhoneNumber, ''), source.PhoneNumber),
               DeliveryAddress = COALESCE(NULLIF(target.DeliveryAddress, ''), source.DeliveryAddress),
               [Role] = 'Client',
               IsActive = 1,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Email, PasswordHash, FirstName, LastName, PhoneNumber, DeliveryAddress, [Role], IsActive)
    VALUES (source.Email, source.PasswordHash, source.FirstName, source.LastName, source.PhoneNumber, source.DeliveryAddress, source.[Role], 1);

DECLARE @AlexUserId INT;
DECLARE @AlexAddress NVARCHAR(500);

SELECT
    @AlexUserId = UserId,
    @AlexAddress = DeliveryAddress
FROM dbo.[User]
WHERE Email = 'alex@u.com';

DECLARE @OrderSeed TABLE (
    OrderCode NVARCHAR(20) PRIMARY KEY,
    DaysAgo INT NOT NULL,
    MinutesOffset INT NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
    Notes NVARCHAR(1000) NULL
);

INSERT INTO @OrderSeed (OrderCode, DaysAgo, MinutesOffset, [Status], Notes)
VALUES
    ('BK-DEMO-001', 0, -35, 'inregistrata', 'Demo active order for manage orders.'),
    ('BK-DEMO-002', 1, -120, 'se pregateste', 'Demo kitchen order.'),
    ('BK-DEMO-003', 2, -210, 'a plecat la client', 'Demo delivery order.'),
    ('BK-DEMO-004', 5, -80, 'livrata', 'Demo delivered order for reports.'),
    ('BK-DEMO-005', 9, -55, 'anulata', 'Demo cancelled order for reports.'),
    ('BK-DEMO-006', 14, -160, 'livrata', 'Second delivered demo order.');

MERGE dbo.[Order] AS target
USING (
    SELECT
        @AlexUserId AS UserId,
        s.OrderCode,
        DATEADD(MINUTE, s.MinutesOffset, DATEADD(DAY, -s.DaysAgo, GETDATE())) AS OrderDate,
        s.[Status],
        @AlexAddress AS DeliveryAddress,
        s.Notes
    FROM @OrderSeed s
) AS source
ON target.OrderCode = source.OrderCode
WHEN MATCHED THEN
    UPDATE SET UserId = source.UserId,
               OrderDate = source.OrderDate,
               [Status] = source.[Status],
               DeliveryAddress = source.DeliveryAddress,
               Notes = source.Notes,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (UserId, OrderCode, OrderDate, [Status], SubTotal, ShippingFee, DiscountAmount, TotalCost, EstimatedDeliveryTime, ActualDeliveryTime, DeliveryAddress, Notes)
    VALUES (source.UserId, source.OrderCode, source.OrderDate, source.[Status], 0, 0, 0, 0, DATEADD(MINUTE, 40, source.OrderDate), NULL, source.DeliveryAddress, source.Notes);

DELETE oi
FROM dbo.OrderItem oi
INNER JOIN dbo.[Order] o ON o.OrderId = oi.OrderId
INNER JOIN @OrderSeed s ON s.OrderCode = o.OrderCode;

DECLARE @ItemSeed TABLE (
    OrderCode NVARCHAR(20) NOT NULL,
    ItemType NVARCHAR(20) NOT NULL,
    ItemName NVARCHAR(150) NOT NULL,
    CategoryName NVARCHAR(100) NULL,
    Quantity INT NOT NULL
);

INSERT INTO @ItemSeed (OrderCode, ItemType, ItemName, CategoryName, Quantity)
VALUES
    ('BK-DEMO-001', 'Meniu', 'Meniu Whopper', 'Meniuri BK', 1),
    ('BK-DEMO-001', 'Preparat', '6 buc. Inele de ceapa', 'Garnituri', 1),
    ('BK-DEMO-002', 'Meniu', 'Meniu Steakhouse', 'Meniuri BK', 2),
    ('BK-DEMO-002', 'Preparat', 'King Sundae Ciocolata', 'Deserturi BK', 2),
    ('BK-DEMO-003', 'Preparat', 'Big King', 'Burgeri', 1),
    ('BK-DEMO-003', 'Preparat', 'Cartofi prajiti portie mica', 'Garnituri', 1),
    ('BK-DEMO-003', 'Preparat', 'Pepsi 0,33l', 'Bauturi BK', 1),
    ('BK-DEMO-004', 'Meniu', 'Meniu Big King', 'Meniuri BK', 2),
    ('BK-DEMO-004', 'Preparat', '9 buc. King Nuggets', 'Pui', 1),
    ('BK-DEMO-004', 'Preparat', 'King Sundae Ciocolata', 'Deserturi BK', 1),
    ('BK-DEMO-005', 'Preparat', 'Whopper', 'Burgeri', 1),
    ('BK-DEMO-005', 'Preparat', 'Pepsi 0,33l', 'Bauturi BK', 1),
    ('BK-DEMO-006', 'Meniu', 'Meniu 9 buc. King Nuggets', 'Meniuri BK', 3),
    ('BK-DEMO-006', 'Preparat', '6 buc. Inele de ceapa', 'Garnituri', 2);

INSERT INTO dbo.OrderItem (OrderId, ProductId, MenuId, ItemType, ItemName, Quantity, UnitPrice, ItemTotal)
SELECT
    o.OrderId,
    p.ProductId,
    NULL,
    'Preparat',
    p.[Name],
    seed.Quantity,
    p.Price,
    CAST(p.Price * seed.Quantity AS DECIMAL(10,2))
FROM @ItemSeed seed
INNER JOIN dbo.[Order] o ON o.OrderCode = seed.OrderCode
INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
INNER JOIN dbo.Product p ON p.[Name] = seed.ItemName AND p.CategoryId = c.CategoryId
WHERE seed.ItemType = 'Preparat';

WITH MenuPrices AS (
    SELECT
        m.MenuId,
        m.[Name],
        CAST(ROUND(
            SUM(p.Price * mp.Quantity) *
            (1 - ((CASE WHEN m.BundleDiscountPercent > 0 THEN m.BundleDiscountPercent ELSE cfg.MenuBundleDiscountPercent END) / 100.0)),
            2
        ) AS DECIMAL(10,2)) AS UnitPrice
    FROM dbo.Menu m
    INNER JOIN dbo.MenuProduct mp ON mp.MenuId = m.MenuId
    INNER JOIN dbo.Product p ON p.ProductId = mp.ProductId
    CROSS JOIN dbo.Configuration cfg
    INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId
    WHERE c.[Name] = 'Meniuri BK'
      AND m.IsDeleted = 0
      AND cfg.ConfigId = 1
    GROUP BY m.MenuId, m.[Name], m.BundleDiscountPercent, cfg.MenuBundleDiscountPercent
)
INSERT INTO dbo.OrderItem (OrderId, ProductId, MenuId, ItemType, ItemName, Quantity, UnitPrice, ItemTotal)
SELECT
    o.OrderId,
    NULL,
    mp.MenuId,
    'Meniu',
    mp.[Name],
    seed.Quantity,
    mp.UnitPrice,
    CAST(mp.UnitPrice * seed.Quantity AS DECIMAL(10,2))
FROM @ItemSeed seed
INNER JOIN dbo.[Order] o ON o.OrderCode = seed.OrderCode
INNER JOIN MenuPrices mp ON mp.[Name] = seed.ItemName
WHERE seed.ItemType = 'Meniu';

DECLARE @MinOrderForFreeShipping DECIMAL(10,2);
DECLARE @ShippingFee DECIMAL(10,2);
DECLARE @LargeOrderDiscountThreshold DECIMAL(10,2);
DECLARE @LargeOrderDiscountPercent DECIMAL(5,2);

SELECT TOP 1
    @MinOrderForFreeShipping = MinOrderForFreeShipping,
    @ShippingFee = ShippingFee,
    @LargeOrderDiscountThreshold = LargeOrderDiscountThreshold,
    @LargeOrderDiscountPercent = LargeOrderDiscountPercent
FROM dbo.Configuration
ORDER BY ConfigId;

WITH Totals AS (
    SELECT
        o.OrderId,
        o.OrderDate,
        o.[Status],
        CAST(SUM(oi.ItemTotal) AS DECIMAL(10,2)) AS SubTotal
    FROM dbo.[Order] o
    INNER JOIN @OrderSeed s ON s.OrderCode = o.OrderCode
    INNER JOIN dbo.OrderItem oi ON oi.OrderId = o.OrderId
    GROUP BY o.OrderId, o.OrderDate, o.[Status]
),
Calculated AS (
    SELECT
        OrderId,
        OrderDate,
        [Status],
        SubTotal,
        CAST(CASE WHEN SubTotal < @MinOrderForFreeShipping THEN @ShippingFee ELSE 0 END AS DECIMAL(10,2)) AS ShippingFee,
        CAST(CASE WHEN SubTotal >= @LargeOrderDiscountThreshold THEN ROUND(SubTotal * @LargeOrderDiscountPercent / 100.0, 2) ELSE 0 END AS DECIMAL(10,2)) AS DiscountAmount
    FROM Totals
)
UPDATE o
SET SubTotal = c.SubTotal,
    ShippingFee = c.ShippingFee,
    DiscountAmount = c.DiscountAmount,
    TotalCost = c.SubTotal + c.ShippingFee - c.DiscountAmount,
    EstimatedDeliveryTime = DATEADD(MINUTE, 40, c.OrderDate),
    ActualDeliveryTime = CASE WHEN c.[Status] = 'livrata' THEN DATEADD(MINUTE, 45, c.OrderDate) ELSE NULL END,
    ModifiedDate = GETDATE()
FROM dbo.[Order] o
INNER JOIN Calculated c ON c.OrderId = o.OrderId;

PRINT 'Burger King-only catalog and Alex demo orders are ready.';

SELECT 'Visible products' AS Entity, COUNT(*) AS [Count]
FROM dbo.Product
WHERE IsDeleted = 0
UNION ALL SELECT 'Visible menus', COUNT(*)
FROM dbo.Menu
WHERE IsDeleted = 0
UNION ALL SELECT 'Alex demo orders', COUNT(*)
FROM dbo.[Order]
WHERE OrderCode LIKE 'BK-DEMO-%'
UNION ALL SELECT 'Low-stock BK products', COUNT(*)
FROM dbo.Product
WHERE IsDeleted = 0 AND TotalQuantity < (SELECT TOP 1 LowStockThreshold FROM dbo.Configuration ORDER BY ConfigId);
GO
