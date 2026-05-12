-- =====================================================
-- Burger King Romania sample menu data
-- Purpose: A compact, realistic set of products and bundled menus
--          with public BK RO Sanity image URLs for local/demo use.
-- =====================================================

USE RestaurantOrderManagement;
GO

SET NOCOUNT ON;

-- Categories aligned with the Burger King Romania public menu.
MERGE dbo.Category AS target
USING (VALUES
    ('Burgeri', 'Sample Burger King burger products'),
    ('Pui', 'Sample Burger King chicken products'),
    ('Garnituri', 'Sample Burger King sides'),
    ('Bauturi BK', 'Sample Burger King beverages'),
    ('Deserturi BK', 'Sample Burger King desserts'),
    ('Meniuri BK', 'Sample Burger King bundled menus')
) AS source ([Name], [Description])
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET [Description] = source.[Description], IsActive = 1
WHEN NOT MATCHED THEN
    INSERT ([Name], [Description], IsActive)
    VALUES (source.[Name], source.[Description], 1);

MERGE dbo.Allergen AS target
USING (VALUES
    ('gluten', 'Cereale care contin gluten'),
    ('lactoza', 'Lapte si produse lactate'),
    ('oua', 'Oua si produse derivate'),
    ('soia', 'Soia si produse derivate'),
    ('mustar', 'Mustar si produse derivate'),
    ('susan', 'Seminte de susan si produse derivate')
) AS source ([Name], [Description])
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET [Description] = source.[Description]
WHEN NOT MATCHED THEN
    INSERT ([Name], [Description])
    VALUES (source.[Name], source.[Description]);

-- Products from Burger King Romania menu, with realistic portions/stocks.
MERGE dbo.Product AS target
USING (
    SELECT c.CategoryId,
           seed.[Name],
           seed.[Description],
           seed.Price,
           seed.PortionQuantity,
           seed.TotalQuantity,
           seed.IsAvailable
    FROM (VALUES
        ('Burgeri', 'Whopper', 'Burger Burger King cu vita la gratar, rosii, salata, maioneza, ketchup, castraveti murati si ceapa.', CAST(27.90 AS DECIMAL(10,2)), 278, 9000, CAST(1 AS BIT)),
        ('Burgeri', 'Big King', 'Burger Burger King cu doua bucati de vita, branza, salata, ceapa, castraveti murati si sos Big King.', CAST(24.90 AS DECIMAL(10,2)), 210, 7600, CAST(1 AS BIT)),
        ('Burgeri', 'Steakhouse', 'Burger cu vita la gratar, bacon, branza, ceapa crocanta, salata si sos BBQ.', CAST(31.90 AS DECIMAL(10,2)), 300, 6200, CAST(1 AS BIT)),
        ('Pui', '6 buc. King Nuggets', 'Bucati crocante de pui Burger King, servite cu sos la alegere.', CAST(17.90 AS DECIMAL(10,2)), 120, 5400, CAST(1 AS BIT)),
        ('Pui', '9 buc. King Nuggets', 'Portie mare de bucati crocante de pui Burger King, servite cu sosuri la alegere.', CAST(21.90 AS DECIMAL(10,2)), 180, 4200, CAST(1 AS BIT)),
        ('Garnituri', 'Cartofi prajiti portie mica', 'Cartofi prajiti Burger King, portie mica.', CAST(8.90 AS DECIMAL(10,2)), 90, 9000, CAST(1 AS BIT)),
        ('Garnituri', '6 buc. Inele de ceapa', 'Inele de ceapa crocante Burger King.', CAST(9.90 AS DECIMAL(10,2)), 90, 4600, CAST(1 AS BIT)),
        ('Bauturi BK', 'Pepsi 0,33l', 'Doza Pepsi 0,33l.', CAST(7.90 AS DECIMAL(10,2)), 330, 12000, CAST(1 AS BIT)),
        ('Deserturi BK', 'King Sundae Ciocolata', 'Inghetata King Sundae cu topping de ciocolata.', CAST(11.90 AS DECIMAL(10,2)), 160, 3600, CAST(1 AS BIT))
    ) AS seed (CategoryName, [Name], [Description], Price, PortionQuantity, TotalQuantity, IsAvailable)
    INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
) AS source
ON target.[Name] = source.[Name] AND target.CategoryId = source.CategoryId
WHEN MATCHED THEN
    UPDATE SET [Description] = source.[Description],
               Price = source.Price,
               PortionQuantity = source.PortionQuantity,
               TotalQuantity = source.TotalQuantity,
               IsAvailable = source.IsAvailable,
               IsDeleted = 0,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CategoryId, [Name], [Description], Price, PortionQuantity, TotalQuantity, IsAvailable, IsDeleted)
    VALUES (source.CategoryId, source.[Name], source.[Description], source.Price, source.PortionQuantity, source.TotalQuantity, source.IsAvailable, 0);

WITH ProductAllergenSeed AS (
    SELECT seed.ProductName, seed.CategoryName, seed.AllergenName
    FROM (VALUES
        ('Whopper', 'Burgeri', 'gluten'),
        ('Whopper', 'Burgeri', 'oua'),
        ('Whopper', 'Burgeri', 'mustar'),
        ('Whopper', 'Burgeri', 'susan'),
        ('Big King', 'Burgeri', 'gluten'),
        ('Big King', 'Burgeri', 'lactoza'),
        ('Big King', 'Burgeri', 'oua'),
        ('Big King', 'Burgeri', 'mustar'),
        ('Big King', 'Burgeri', 'susan'),
        ('Steakhouse', 'Burgeri', 'gluten'),
        ('Steakhouse', 'Burgeri', 'lactoza'),
        ('Steakhouse', 'Burgeri', 'oua'),
        ('Steakhouse', 'Burgeri', 'soia'),
        ('Steakhouse', 'Burgeri', 'mustar'),
        ('Steakhouse', 'Burgeri', 'susan'),
        ('6 buc. King Nuggets', 'Pui', 'gluten'),
        ('6 buc. King Nuggets', 'Pui', 'oua'),
        ('9 buc. King Nuggets', 'Pui', 'gluten'),
        ('9 buc. King Nuggets', 'Pui', 'oua'),
        ('Cartofi prajiti portie mica', 'Garnituri', 'gluten'),
        ('6 buc. Inele de ceapa', 'Garnituri', 'gluten'),
        ('6 buc. Inele de ceapa', 'Garnituri', 'lactoza'),
        ('King Sundae Ciocolata', 'Deserturi BK', 'lactoza')
    ) AS seed (ProductName, CategoryName, AllergenName)
)
INSERT INTO dbo.ProductAllergen (ProductId, AllergenId)
SELECT p.ProductId, a.AllergenId
FROM ProductAllergenSeed seed
INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName AND p.CategoryId = c.CategoryId
INNER JOIN dbo.Allergen a ON a.[Name] = seed.AllergenName
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ProductAllergen pa
    WHERE pa.ProductId = p.ProductId
      AND pa.AllergenId = a.AllergenId
);

DELETE pi
FROM dbo.ProductImage pi
INNER JOIN dbo.Product p ON p.ProductId = pi.ProductId
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
WHERE c.[Name] IN ('Burgeri', 'Pui', 'Garnituri', 'Bauturi BK', 'Deserturi BK')
  AND p.[Name] IN (
      'Whopper',
      'Big King',
      'Steakhouse',
      '6 buc. King Nuggets',
      '9 buc. King Nuggets',
      'Cartofi prajiti portie mica',
      '6 buc. Inele de ceapa',
      'Pepsi 0,33l',
      'King Sundae Ciocolata'
  );

WITH ProductImageSeed AS (
    SELECT seed.ProductName, seed.CategoryName, seed.ImageUrl, seed.DisplayOrder
    FROM (VALUES
        ('Whopper', 'Burgeri', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/43fcc2a7a5ccbe348a5b2977cf0e0165d78ca759-676x450.png', 1),
        ('Big King', 'Burgeri', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/6b49163a07ee16b129f61243e21c18e2b4e86540-676x450.png', 1),
        ('Steakhouse', 'Burgeri', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/2a78a1cf3006ab36110d76b9dd3d239b2176d95d-676x450.png', 1),
        ('6 buc. King Nuggets', 'Pui', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/b01382dd542f97f7477a3ead8f57c06e92ca4fb4-676x450.png', 1),
        ('9 buc. King Nuggets', 'Pui', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/02c6f69bcaeb367d48891c1af8cbe7630654811c-676x450.png', 1),
        ('Cartofi prajiti portie mica', 'Garnituri', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/d58396d61997c0d2587615daf124ee947024ad19-676x450.png', 1),
        ('6 buc. Inele de ceapa', 'Garnituri', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/3454f38fc4579ed3a51097db05f3a95cbf140648-676x450.png', 1),
        ('Pepsi 0,33l', 'Bauturi BK', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/c9d555c17d6557243c0170e40ef512f4153e4469-675x450.png', 1),
        ('King Sundae Ciocolata', 'Deserturi BK', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/a2125c628d79d38bcc31fd80aebfa2a7a8b06399-676x450.png', 1)
    ) AS seed (ProductName, CategoryName, ImageUrl, DisplayOrder)
)
INSERT INTO dbo.ProductImage (ProductId, ImageUrl, DisplayOrder)
SELECT p.ProductId, seed.ImageUrl, seed.DisplayOrder
FROM ProductImageSeed seed
INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName AND p.CategoryId = c.CategoryId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ProductImage pi
    WHERE pi.ProductId = p.ProductId
      AND pi.ImageUrl = seed.ImageUrl
);

-- Bundled menus. The app computes final menu prices from component prices
-- and BundleDiscountPercent, matching the project requirement.
MERGE dbo.Menu AS target
USING (
    SELECT c.CategoryId,
           seed.[Name],
           seed.[Description],
           seed.BundleDiscountPercent,
           seed.IsAvailable
    FROM (VALUES
        ('Meniuri BK', 'Meniu Whopper', 'Whopper, cartofi prajiti portie mica si Pepsi 0,33l.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Meniuri BK', 'Meniu Big King', 'Big King, cartofi prajiti portie mica si Pepsi 0,33l.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Meniuri BK', 'Meniu Steakhouse', 'Steakhouse, cartofi prajiti portie mica si Pepsi 0,33l.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Meniuri BK', 'Meniu 9 buc. King Nuggets', 'King Nuggets, cartofi prajiti portie mica si Pepsi 0,33l.', CAST(8.00 AS DECIMAL(5,2)), CAST(1 AS BIT))
    ) AS seed (CategoryName, [Name], [Description], BundleDiscountPercent, IsAvailable)
    INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
) AS source
ON target.[Name] = source.[Name] AND target.CategoryId = source.CategoryId
WHEN MATCHED THEN
    UPDATE SET [Description] = source.[Description],
               BundleDiscountPercent = source.BundleDiscountPercent,
               IsAvailable = source.IsAvailable,
               IsDeleted = 0,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CategoryId, [Name], [Description], BundleDiscountPercent, IsAvailable, IsDeleted)
    VALUES (source.CategoryId, source.[Name], source.[Description], source.BundleDiscountPercent, source.IsAvailable, 0);

DELETE mp
FROM dbo.MenuProduct mp
INNER JOIN dbo.Menu m ON m.MenuId = mp.MenuId
INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId
WHERE c.[Name] = 'Meniuri BK'
  AND m.[Name] IN ('Meniu Whopper', 'Meniu Big King', 'Meniu Steakhouse', 'Meniu 9 buc. King Nuggets');

WITH MenuProductSeed AS (
    SELECT seed.MenuName, seed.ProductName, seed.ProductCategoryName, seed.Quantity
    FROM (VALUES
        ('Meniu Whopper', 'Whopper', 'Burgeri', 1),
        ('Meniu Whopper', 'Cartofi prajiti portie mica', 'Garnituri', 1),
        ('Meniu Whopper', 'Pepsi 0,33l', 'Bauturi BK', 1),
        ('Meniu Big King', 'Big King', 'Burgeri', 1),
        ('Meniu Big King', 'Cartofi prajiti portie mica', 'Garnituri', 1),
        ('Meniu Big King', 'Pepsi 0,33l', 'Bauturi BK', 1),
        ('Meniu Steakhouse', 'Steakhouse', 'Burgeri', 1),
        ('Meniu Steakhouse', 'Cartofi prajiti portie mica', 'Garnituri', 1),
        ('Meniu Steakhouse', 'Pepsi 0,33l', 'Bauturi BK', 1),
        ('Meniu 9 buc. King Nuggets', '9 buc. King Nuggets', 'Pui', 1),
        ('Meniu 9 buc. King Nuggets', 'Cartofi prajiti portie mica', 'Garnituri', 1),
        ('Meniu 9 buc. King Nuggets', 'Pepsi 0,33l', 'Bauturi BK', 1)
    ) AS seed (MenuName, ProductName, ProductCategoryName, Quantity)
)
MERGE dbo.MenuProduct AS target
USING (
    SELECT m.MenuId, p.ProductId, seed.Quantity
    FROM MenuProductSeed seed
    INNER JOIN dbo.Category menuCategory ON menuCategory.[Name] = 'Meniuri BK'
    INNER JOIN dbo.Menu m ON m.[Name] = seed.MenuName AND m.CategoryId = menuCategory.CategoryId
    INNER JOIN dbo.Category productCategory ON productCategory.[Name] = seed.ProductCategoryName
    INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName AND p.CategoryId = productCategory.CategoryId
) AS source
ON target.MenuId = source.MenuId AND target.ProductId = source.ProductId
WHEN MATCHED THEN
    UPDATE SET Quantity = source.Quantity
WHEN NOT MATCHED THEN
    INSERT (MenuId, ProductId, Quantity)
    VALUES (source.MenuId, source.ProductId, source.Quantity);

DELETE mi
FROM dbo.MenuImage mi
INNER JOIN dbo.Menu m ON m.MenuId = mi.MenuId
INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId
WHERE c.[Name] = 'Meniuri BK'
  AND m.[Name] IN ('Meniu Whopper', 'Meniu Big King', 'Meniu Steakhouse', 'Meniu 9 buc. King Nuggets');

WITH MenuImageSeed AS (
    SELECT seed.MenuName, seed.ImageUrl, seed.DisplayOrder
    FROM (VALUES
        ('Meniu Whopper', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/7b97354506e5f84b0bb3d6cec84a5e34d84cbeed-676x450.png', 1),
        ('Meniu Big King', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/bbb64eef5b949ec877f8eec4866606110980480d-676x450.png', 1),
        ('Meniu Steakhouse', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/ac71acebd73061289550be600945ce83140eed16-676x450.png', 1),
        ('Meniu 9 buc. King Nuggets', 'https://cdn.sanity.io/images/czqk28jt/prod_bk_ro/982eeb76a3d1fb410a01b289a140d468d26aad0a-676x450.png', 1)
    ) AS seed (MenuName, ImageUrl, DisplayOrder)
)
INSERT INTO dbo.MenuImage (MenuId, ImageUrl, DisplayOrder)
SELECT m.MenuId, seed.ImageUrl, seed.DisplayOrder
FROM MenuImageSeed seed
INNER JOIN dbo.Category c ON c.[Name] = 'Meniuri BK'
INNER JOIN dbo.Menu m ON m.[Name] = seed.MenuName AND m.CategoryId = c.CategoryId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.MenuImage mi
    WHERE mi.MenuId = m.MenuId
      AND mi.ImageUrl = seed.ImageUrl
);

PRINT 'Burger King sample menu data is ready.';

SELECT 'BK categories' AS Entity, COUNT(*) AS [Count]
FROM dbo.Category
WHERE [Name] IN ('Burgeri', 'Pui', 'Garnituri', 'Bauturi BK', 'Deserturi BK', 'Meniuri BK')
UNION ALL SELECT 'BK products', COUNT(*)
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
WHERE c.[Name] IN ('Burgeri', 'Pui', 'Garnituri', 'Bauturi BK', 'Deserturi BK') AND p.IsDeleted = 0
UNION ALL SELECT 'BK product images', COUNT(*)
FROM dbo.ProductImage pi
INNER JOIN dbo.Product p ON p.ProductId = pi.ProductId
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
WHERE c.[Name] IN ('Burgeri', 'Pui', 'Garnituri', 'Bauturi BK', 'Deserturi BK')
UNION ALL SELECT 'BK menus', COUNT(*)
FROM dbo.Menu m
INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId
WHERE c.[Name] = 'Meniuri BK' AND m.IsDeleted = 0
UNION ALL SELECT 'BK menu images', COUNT(*)
FROM dbo.MenuImage mi
INNER JOIN dbo.Menu m ON m.MenuId = mi.MenuId
INNER JOIN dbo.Category c ON c.CategoryId = m.CategoryId
WHERE c.[Name] = 'Meniuri BK';
GO
