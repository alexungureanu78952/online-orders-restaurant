-- =====================================================
-- Sample Data for Restaurant Menu Display and Search
-- Purpose: Products from every category, bundled menus, images,
-- allergens, portion quantities, stock quantities and unavailable state.
-- =====================================================

USE RestaurantOrderManagement;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Configuration)
    INSERT INTO dbo.Configuration DEFAULT VALUES;

UPDATE dbo.Configuration
SET MinOrderForFreeShipping = 100,
    ShippingFee = 15,
    LargeOrderDiscountThreshold = 200,
    LargeOrderDiscountPercent = 10,
    MenuBundleDiscountPercent = 15,
    FrequentOrderThreshold = 5,
    FrequentOrderTimeWindow = 30,
    FrequentOrderDiscountPercent = 8,
    LowStockThreshold = 500,
    ModifiedDate = GETDATE();

-- Categories from the project brief.
MERGE dbo.Category AS target
USING (VALUES
    ('Mic dejun', 'Preparate usoare pentru inceputul zilei'),
    ('Aperitive', 'Gustari si platouri de impartit'),
    ('Supe si ciorbe', 'Preparate calde la bol'),
    ('Feluri principale', 'Preparate consistente pentru pranz si cina'),
    ('Salate si garnituri', 'Garnituri, salate si acompaniamente'),
    ('Deserturi', 'Dulciuri pregatite in restaurant'),
    ('Bauturi', 'Bauturi reci si calde')
) AS source ([Name], [Description])
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET [Description] = source.[Description], IsActive = 1
WHEN NOT MATCHED THEN
    INSERT ([Name], [Description], IsActive)
    VALUES (source.[Name], source.[Description], 1);

-- Legacy category from early planning. Bundled menus are now grouped in their real food categories.
IF EXISTS (SELECT 1 FROM dbo.Category WHERE [Name] = 'Meniuri')
    UPDATE dbo.Category
    SET IsActive = 0,
        [Description] = 'Categorie pastrata pentru compatibilitate; meniurile sunt grupate pe categoria lor culinara.'
    WHERE [Name] = 'Meniuri';

-- Allergens used by the seeded products and menus.
MERGE dbo.Allergen AS target
USING (VALUES
    ('gluten', 'Cereale care contin gluten'),
    ('lactoza', 'Lapte si produse lactate'),
    ('oua', 'Oua si produse derivate'),
    ('peste', 'Peste si produse din peste'),
    ('telina', 'Telina si produse derivate'),
    ('nuci', 'Fructe cu coaja lemnoasa'),
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

-- Products: the public restaurant menu contains every active product,
-- grouped by category, with portion quantity and total stock.
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
        ('Mic dejun', 'Omleta cu legume', 'Omleta pufoasa cu ardei, rosii, ceapa verde si cascaval.', CAST(22.00 AS DECIMAL(10,2)), 180, 4200, CAST(1 AS BIT)),
        ('Mic dejun', 'Croissant cu unt', 'Croissant cald cu unt si textura frageda.', CAST(10.00 AS DECIMAL(10,2)), 80, 2400, CAST(1 AS BIT)),
        ('Mic dejun', 'Iaurt cu granola', 'Iaurt cremos cu granola crocanta si fructe de sezon.', CAST(16.00 AS DECIMAL(10,2)), 200, 3600, CAST(1 AS BIT)),
        ('Aperitive', 'Bruschete cu rosii', 'Paine prajita cu rosii proaspete, usturoi si busuioc.', CAST(17.00 AS DECIMAL(10,2)), 180, 3200, CAST(1 AS BIT)),
        ('Aperitive', 'Hummus cu lipie', 'Hummus fin cu lipie calda si ulei de masline.', CAST(19.00 AS DECIMAL(10,2)), 220, 4000, CAST(1 AS BIT)),
        ('Aperitive', 'Platou branzeturi', 'Selectie de branzeturi, nuci si fructe uscate.', CAST(34.00 AS DECIMAL(10,2)), 250, 2800, CAST(1 AS BIT)),
        ('Supe si ciorbe', 'Supa crema de ciuperci', 'Supa fina cu crutoane si smantana.', CAST(18.00 AS DECIMAL(10,2)), 300, 3700, CAST(1 AS BIT)),
        ('Supe si ciorbe', 'Ciorba de burta', 'Ciorba traditionala cu smantana, ou si ardei iute.', CAST(24.00 AS DECIMAL(10,2)), 350, 4200, CAST(1 AS BIT)),
        ('Supe si ciorbe', 'Supa de rosii cu busuioc', 'Supa lejera de rosii coapte, busuioc si ulei de masline.', CAST(17.00 AS DECIMAL(10,2)), 300, 4500, CAST(1 AS BIT)),
        ('Feluri principale', 'Pastrav la gratar', 'Pastrav rumenit, servit cu lamaie.', CAST(32.00 AS DECIMAL(10,2)), 200, 1400, CAST(1 AS BIT)),
        ('Feluri principale', 'Piept de pui la gratar', 'Piept de pui marinat, gatit pe gratar.', CAST(31.00 AS DECIMAL(10,2)), 220, 5000, CAST(1 AS BIT)),
        ('Feluri principale', 'Snitel de pui', 'Snitel crocant din piept de pui cu pesmet auriu.', CAST(29.00 AS DECIMAL(10,2)), 220, 3300, CAST(1 AS BIT)),
        ('Feluri principale', 'Burger de vita', 'Burger cu vita, chifla rumenita, cascaval, sos si legume.', CAST(38.00 AS DECIMAL(10,2)), 300, 3600, CAST(1 AS BIT)),
        ('Feluri principale', 'Paste carbonara', 'Paste cu sos cremos, bacon si parmezan.', CAST(35.00 AS DECIMAL(10,2)), 320, 3840, CAST(1 AS BIT)),
        ('Feluri principale', 'Aripioare picante', 'Aripioare crocante in crusta picanta.', CAST(28.00 AS DECIMAL(10,2)), 250, 0, CAST(0 AS BIT)),
        ('Salate si garnituri', 'Cartofi prajiti', 'Cartofi crocanti cu sare.', CAST(12.00 AS DECIMAL(10,2)), 200, 5000, CAST(1 AS BIT)),
        ('Salate si garnituri', 'Salata de varza', 'Varza alba si rosie cu dressing usor acrisor.', CAST(11.00 AS DECIMAL(10,2)), 150, 3000, CAST(1 AS BIT)),
        ('Salate si garnituri', 'Orez cu legume', 'Orez pufos cu morcov, mazare si ardei.', CAST(13.00 AS DECIMAL(10,2)), 200, 3600, CAST(1 AS BIT)),
        ('Salate si garnituri', 'Cartofi copti cu rozmarin', 'Cartofi copti lent cu rozmarin si usturoi.', CAST(14.00 AS DECIMAL(10,2)), 220, 3080, CAST(1 AS BIT)),
        ('Deserturi', 'Papanasi cu smantana', 'Papanasi rumeni cu smantana si dulceata de afine.', CAST(24.00 AS DECIMAL(10,2)), 220, 2640, CAST(1 AS BIT)),
        ('Deserturi', 'Clatite cu ciocolata', 'Clatite subtiri umplute cu crema de ciocolata.', CAST(20.00 AS DECIMAL(10,2)), 180, 2880, CAST(1 AS BIT)),
        ('Deserturi', 'Sorbet de zmeura', 'Sorbet racoritor din zmeura, fara lactoza.', CAST(14.00 AS DECIMAL(10,2)), 120, 1800, CAST(1 AS BIT)),
        ('Bauturi', 'Pepsi', 'Doza rece.', CAST(8.00 AS DECIMAL(10,2)), 330, 12000, CAST(1 AS BIT)),
        ('Bauturi', 'Fanta', 'Doza rece cu aroma de portocale.', CAST(8.00 AS DECIMAL(10,2)), 330, 11000, CAST(1 AS BIT)),
        ('Bauturi', 'Ceai de tei', 'Ceai cald de tei cu miere optionala.', CAST(9.00 AS DECIMAL(10,2)), 250, 6000, CAST(1 AS BIT)),
        ('Bauturi', 'Limonada cu menta', 'Limonada proaspata cu lamaie si menta.', CAST(12.00 AS DECIMAL(10,2)), 400, 8000, CAST(1 AS BIT)),
        ('Bauturi', 'Cappuccino', 'Cafea espresso cu spuma fina de lapte.', CAST(13.00 AS DECIMAL(10,2)), 180, 3600, CAST(1 AS BIT))
    ) AS seed (CategoryName, [Name], [Description], Price, PortionQuantity, TotalQuantity, IsAvailable)
    INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
) AS source
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET CategoryId = source.CategoryId,
               [Description] = source.[Description],
               Price = source.Price,
               PortionQuantity = source.PortionQuantity,
               TotalQuantity = source.TotalQuantity,
               IsAvailable = source.IsAvailable,
               IsDeleted = 0,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CategoryId, [Name], [Description], Price, PortionQuantity, TotalQuantity, IsAvailable, IsDeleted)
    VALUES (source.CategoryId, source.[Name], source.[Description], source.Price, source.PortionQuantity, source.TotalQuantity, source.IsAvailable, 0);

-- Product-allergen associations. Products absent from this list intentionally have no allergens.
WITH ProductAllergenSeed AS (
    SELECT seed.ProductName, seed.AllergenName
    FROM (VALUES
        ('Omleta cu legume', 'oua'),
        ('Omleta cu legume', 'lactoza'),
        ('Croissant cu unt', 'gluten'),
        ('Croissant cu unt', 'lactoza'),
        ('Iaurt cu granola', 'gluten'),
        ('Iaurt cu granola', 'lactoza'),
        ('Iaurt cu granola', 'nuci'),
        ('Bruschete cu rosii', 'gluten'),
        ('Hummus cu lipie', 'gluten'),
        ('Hummus cu lipie', 'susan'),
        ('Platou branzeturi', 'lactoza'),
        ('Platou branzeturi', 'nuci'),
        ('Supa crema de ciuperci', 'lactoza'),
        ('Supa crema de ciuperci', 'gluten'),
        ('Ciorba de burta', 'lactoza'),
        ('Ciorba de burta', 'oua'),
        ('Pastrav la gratar', 'peste'),
        ('Snitel de pui', 'gluten'),
        ('Snitel de pui', 'oua'),
        ('Burger de vita', 'gluten'),
        ('Burger de vita', 'lactoza'),
        ('Burger de vita', 'oua'),
        ('Burger de vita', 'susan'),
        ('Paste carbonara', 'gluten'),
        ('Paste carbonara', 'lactoza'),
        ('Paste carbonara', 'oua'),
        ('Aripioare picante', 'gluten'),
        ('Aripioare picante', 'oua'),
        ('Aripioare picante', 'mustar'),
        ('Papanasi cu smantana', 'gluten'),
        ('Papanasi cu smantana', 'lactoza'),
        ('Papanasi cu smantana', 'oua'),
        ('Clatite cu ciocolata', 'gluten'),
        ('Clatite cu ciocolata', 'lactoza'),
        ('Clatite cu ciocolata', 'oua'),
        ('Clatite cu ciocolata', 'nuci'),
        ('Cappuccino', 'lactoza')
    ) AS seed (ProductName, AllergenName)
)
INSERT INTO dbo.ProductAllergen (ProductId, AllergenId)
SELECT p.ProductId, a.AllergenId
FROM ProductAllergenSeed seed
INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName
INNER JOIN dbo.Allergen a ON a.[Name] = seed.AllergenName
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ProductAllergen pa
    WHERE pa.ProductId = p.ProductId
      AND pa.AllergenId = a.AllergenId
);

-- Product image galleries. Several products have multiple images so the UI gallery is visible.
DELETE FROM dbo.ProductImage
WHERE ImageUrl LIKE 'https://placehold.co/%'
   OR ImageUrl LIKE 'https://loremflickr.com/%';

WITH ProductImageSeed AS (
    SELECT seed.ProductName, seed.ImageUrl, seed.DisplayOrder
    FROM (VALUES
        ('Omleta cu legume', 'https://loremflickr.com/420/280/omelette,breakfast,food?lock=1001', 1),
        ('Omleta cu legume', 'https://loremflickr.com/420/280/eggs,vegetables,food?lock=1002', 2),
        ('Croissant cu unt', 'https://loremflickr.com/420/280/croissant,bakery,food?lock=1003', 1),
        ('Iaurt cu granola', 'https://loremflickr.com/420/280/yogurt,granola,breakfast?lock=1004', 1),
        ('Bruschete cu rosii', 'https://loremflickr.com/420/280/bruschetta,tomato,food?lock=1005', 1),
        ('Hummus cu lipie', 'https://loremflickr.com/420/280/hummus,pita,food?lock=1006', 1),
        ('Platou branzeturi', 'https://loremflickr.com/420/280/cheese,platter,food?lock=1007', 1),
        ('Supa crema de ciuperci', 'https://loremflickr.com/420/280/mushroom,soup,food?lock=1008', 1),
        ('Supa crema de ciuperci', 'https://loremflickr.com/420/280/cream,soup,food?lock=1009', 2),
        ('Ciorba de burta', 'https://loremflickr.com/420/280/soup,traditional,food?lock=1010', 1),
        ('Supa de rosii cu busuioc', 'https://loremflickr.com/420/280/tomato,soup,basil?lock=1011', 1),
        ('Pastrav la gratar', 'https://loremflickr.com/420/280/grilled,fish,food?lock=1012', 1),
        ('Pastrav la gratar', 'https://loremflickr.com/420/280/trout,lemon,food?lock=1013', 2),
        ('Piept de pui la gratar', 'https://loremflickr.com/420/280/grilled,chicken,food?lock=1014', 1),
        ('Snitel de pui', 'https://loremflickr.com/420/280/chicken,schnitzel,food?lock=1015', 1),
        ('Burger de vita', 'https://loremflickr.com/420/280/beef,burger,food?lock=1016', 1),
        ('Burger de vita', 'https://loremflickr.com/420/280/burger,fries,food?lock=1017', 2),
        ('Paste carbonara', 'https://loremflickr.com/420/280/carbonara,pasta,food?lock=1018', 1),
        ('Aripioare picante', 'https://loremflickr.com/420/280/spicy,wings,food?lock=1019', 1),
        ('Cartofi prajiti', 'https://loremflickr.com/420/280/french,fries,food?lock=1020', 1),
        ('Salata de varza', 'https://loremflickr.com/420/280/cabbage,salad,food?lock=1021', 1),
        ('Orez cu legume', 'https://loremflickr.com/420/280/rice,vegetables,food?lock=1022', 1),
        ('Cartofi copti cu rozmarin', 'https://loremflickr.com/420/280/roasted,potatoes,food?lock=1023', 1),
        ('Papanasi cu smantana', 'https://loremflickr.com/420/280/doughnuts,dessert,food?lock=1024', 1),
        ('Papanasi cu smantana', 'https://loremflickr.com/420/280/berry,dessert,food?lock=1025', 2),
        ('Clatite cu ciocolata', 'https://loremflickr.com/420/280/pancakes,chocolate,food?lock=1026', 1),
        ('Sorbet de zmeura', 'https://loremflickr.com/420/280/raspberry,sorbet,dessert?lock=1027', 1),
        ('Pepsi', 'https://loremflickr.com/420/280/cola,soda,drink?lock=1028', 1),
        ('Fanta', 'https://loremflickr.com/420/280/orange,soda,drink?lock=1029', 1),
        ('Ceai de tei', 'https://loremflickr.com/420/280/herbal,tea,drink?lock=1030', 1),
        ('Limonada cu menta', 'https://loremflickr.com/420/280/lemonade,mint,drink?lock=1031', 1),
        ('Cappuccino', 'https://loremflickr.com/420/280/cappuccino,coffee,drink?lock=1032', 1)
    ) AS seed (ProductName, ImageUrl, DisplayOrder)
)
INSERT INTO dbo.ProductImage (ProductId, ImageUrl, DisplayOrder)
SELECT p.ProductId, seed.ImageUrl, seed.DisplayOrder
FROM ProductImageSeed seed
INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ProductImage pi
    WHERE pi.ProductId = p.ProductId
      AND pi.ImageUrl = seed.ImageUrl
);

-- Bundled menus. They are still restaurant menu items, but ItemType = 'Meniu'.
MERGE dbo.Menu AS target
USING (
    SELECT c.CategoryId,
           seed.[Name],
           seed.[Description],
           seed.BundleDiscountPercent,
           seed.IsAvailable
    FROM (VALUES
        ('Mic dejun', 'Meniu mic dejun complet', 'Omleta cu legume, croissant cu unt si ceai de tei.', CAST(12.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Fish and chips', 'Pastrav la gratar cu portie de cartofi prajiti.', CAST(15.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu burger', 'Burger de vita, cartofi prajiti si Pepsi.', CAST(15.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu pui clasic', 'Piept de pui la gratar, orez cu legume si limonada.', CAST(12.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu aripioare', 'Aripioare picante cu cartofi si bautura. Devine indisponibil cand aripioarele nu au stoc.', CAST(15.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Supe si ciorbe', 'Meniu ciorba si snitel', 'Ciorba de burta, snitel de pui si salata de varza.', CAST(12.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu carbonara dulce', 'Paste carbonara, limonada cu menta si papanasi cu smantana.', CAST(12.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu kids pui', 'Snitel de pui, cartofi prajiti si Fanta.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Feluri principale', 'Meniu pescar complet', 'Pastrav la gratar, cartofi copti cu rozmarin si limonada.', CAST(14.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Aperitive', 'Meniu vegan fresh', 'Hummus cu lipie, salata de varza si limonada cu menta.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT)),
        ('Deserturi', 'Desert pentru doi', 'Papanasi cu smantana si clatite cu ciocolata.', CAST(10.00 AS DECIMAL(5,2)), CAST(1 AS BIT))
    ) AS seed (CategoryName, [Name], [Description], BundleDiscountPercent, IsAvailable)
    INNER JOIN dbo.Category c ON c.[Name] = seed.CategoryName
) AS source
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET CategoryId = source.CategoryId,
               [Description] = source.[Description],
               BundleDiscountPercent = source.BundleDiscountPercent,
               IsAvailable = source.IsAvailable,
               IsDeleted = 0,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CategoryId, [Name], [Description], BundleDiscountPercent, IsAvailable, IsDeleted)
    VALUES (source.CategoryId, source.[Name], source.[Description], source.BundleDiscountPercent, source.IsAvailable, 0);

WITH MenuProductSeed AS (
    SELECT seed.MenuName, seed.ProductName, seed.Quantity
    FROM (VALUES
        ('Meniu mic dejun complet', 'Omleta cu legume', 1),
        ('Meniu mic dejun complet', 'Croissant cu unt', 1),
        ('Meniu mic dejun complet', 'Ceai de tei', 1),
        ('Fish and chips', 'Pastrav la gratar', 1),
        ('Fish and chips', 'Cartofi prajiti', 1),
        ('Meniu burger', 'Burger de vita', 1),
        ('Meniu burger', 'Cartofi prajiti', 1),
        ('Meniu burger', 'Pepsi', 1),
        ('Meniu pui clasic', 'Piept de pui la gratar', 1),
        ('Meniu pui clasic', 'Orez cu legume', 1),
        ('Meniu pui clasic', 'Limonada cu menta', 1),
        ('Meniu aripioare', 'Aripioare picante', 1),
        ('Meniu aripioare', 'Cartofi prajiti', 1),
        ('Meniu aripioare', 'Pepsi', 1),
        ('Meniu ciorba si snitel', 'Ciorba de burta', 1),
        ('Meniu ciorba si snitel', 'Snitel de pui', 1),
        ('Meniu ciorba si snitel', 'Salata de varza', 1),
        ('Meniu carbonara dulce', 'Paste carbonara', 1),
        ('Meniu carbonara dulce', 'Limonada cu menta', 1),
        ('Meniu carbonara dulce', 'Papanasi cu smantana', 1),
        ('Meniu kids pui', 'Snitel de pui', 1),
        ('Meniu kids pui', 'Cartofi prajiti', 1),
        ('Meniu kids pui', 'Fanta', 1),
        ('Meniu pescar complet', 'Pastrav la gratar', 1),
        ('Meniu pescar complet', 'Cartofi copti cu rozmarin', 1),
        ('Meniu pescar complet', 'Limonada cu menta', 1),
        ('Meniu vegan fresh', 'Hummus cu lipie', 1),
        ('Meniu vegan fresh', 'Salata de varza', 1),
        ('Meniu vegan fresh', 'Limonada cu menta', 1),
        ('Desert pentru doi', 'Papanasi cu smantana', 1),
        ('Desert pentru doi', 'Clatite cu ciocolata', 1)
    ) AS seed (MenuName, ProductName, Quantity)
)
MERGE dbo.MenuProduct AS target
USING (
    SELECT m.MenuId, p.ProductId, seed.Quantity
    FROM MenuProductSeed seed
    INNER JOIN dbo.Menu m ON m.[Name] = seed.MenuName
    INNER JOIN dbo.Product p ON p.[Name] = seed.ProductName
) AS source
ON target.MenuId = source.MenuId AND target.ProductId = source.ProductId
WHEN MATCHED THEN
    UPDATE SET Quantity = source.Quantity
WHEN NOT MATCHED THEN
    INSERT (MenuId, ProductId, Quantity)
    VALUES (source.MenuId, source.ProductId, source.Quantity);

DELETE FROM dbo.MenuImage
WHERE ImageUrl LIKE 'https://placehold.co/%'
   OR ImageUrl LIKE 'https://loremflickr.com/%';

WITH MenuImageSeed AS (
    SELECT seed.MenuName, seed.ImageUrl, seed.DisplayOrder
    FROM (VALUES
        ('Meniu mic dejun complet', 'https://loremflickr.com/420/280/breakfast,meal,food?lock=2001', 1),
        ('Fish and chips', 'https://loremflickr.com/420/280/fish,chips,food?lock=2002', 1),
        ('Meniu burger', 'https://loremflickr.com/420/280/burger,meal,food?lock=2003', 1),
        ('Meniu burger', 'https://loremflickr.com/420/280/burger,fries,drink?lock=2004', 2),
        ('Meniu pui clasic', 'https://loremflickr.com/420/280/chicken,rice,meal?lock=2005', 1),
        ('Meniu aripioare', 'https://loremflickr.com/420/280/chicken,wings,fries?lock=2006', 1),
        ('Meniu ciorba si snitel', 'https://loremflickr.com/420/280/soup,schnitzel,meal?lock=2007', 1),
        ('Meniu carbonara dulce', 'https://loremflickr.com/420/280/pasta,dessert,meal?lock=2008', 1),
        ('Meniu kids pui', 'https://loremflickr.com/420/280/kids,meal,chicken?lock=2009', 1),
        ('Meniu pescar complet', 'https://loremflickr.com/420/280/grilled,fish,meal?lock=2010', 1),
        ('Meniu vegan fresh', 'https://loremflickr.com/420/280/vegan,lunch,food?lock=2011', 1),
        ('Desert pentru doi', 'https://loremflickr.com/420/280/dessert,plate,food?lock=2012', 1)
    ) AS seed (MenuName, ImageUrl, DisplayOrder)
)
INSERT INTO dbo.MenuImage (MenuId, ImageUrl, DisplayOrder)
SELECT m.MenuId, seed.ImageUrl, seed.DisplayOrder
FROM MenuImageSeed seed
INNER JOIN dbo.Menu m ON m.[Name] = seed.MenuName
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.MenuImage mi
    WHERE mi.MenuId = m.MenuId
      AND mi.ImageUrl = seed.ImageUrl
);

PRINT 'Sample restaurant menu data is ready.';

SELECT 'Categories' AS Entity, COUNT(*) AS [Count] FROM dbo.Category WHERE IsActive = 1
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Product WHERE IsDeleted = 0
UNION ALL SELECT 'Menus', COUNT(*) FROM dbo.Menu WHERE IsDeleted = 0
UNION ALL SELECT 'Allergens', COUNT(*) FROM dbo.Allergen;
GO
