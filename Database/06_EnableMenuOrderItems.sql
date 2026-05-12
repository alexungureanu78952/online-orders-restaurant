-- =====================================================
-- Order Items Support for Products and Bundled Menus
-- Purpose: Allow orders to contain both preparate and meniuri.
-- =====================================================

USE RestaurantOrderManagement;
GO

SET NOCOUNT ON;

DECLARE @fkName NVARCHAR(128);
DECLARE @dropFkSql NVARCHAR(MAX);

SELECT @fkName = fk.name
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables child ON fk.parent_object_id = child.object_id
INNER JOIN sys.columns childColumn ON fkc.parent_object_id = childColumn.object_id
    AND fkc.parent_column_id = childColumn.column_id
WHERE child.name = 'OrderItem'
    AND childColumn.name = 'ProductId';

IF @fkName IS NOT NULL
BEGIN
    SET @dropFkSql = N'ALTER TABLE dbo.OrderItem DROP CONSTRAINT ' + QUOTENAME(@fkName);
    EXEC sp_executesql @dropFkSql;
END;
GO

IF COL_LENGTH('dbo.OrderItem', 'MenuId') IS NULL
    ALTER TABLE dbo.OrderItem ADD MenuId INT NULL;
GO

IF COL_LENGTH('dbo.OrderItem', 'ItemType') IS NULL
    ALTER TABLE dbo.OrderItem ADD ItemType NVARCHAR(20) NOT NULL CONSTRAINT DF_OrderItem_ItemType DEFAULT 'Preparat';
GO

IF COL_LENGTH('dbo.OrderItem', 'ItemName') IS NULL
    ALTER TABLE dbo.OrderItem ADD ItemName NVARCHAR(150) NOT NULL CONSTRAINT DF_OrderItem_ItemName DEFAULT '';
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.OrderItem')
        AND name = 'ProductId'
        AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.OrderItem ALTER COLUMN ProductId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItem_Product')
BEGIN
    ALTER TABLE dbo.OrderItem
    ADD CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderItem_Menu')
BEGIN
    ALTER TABLE dbo.OrderItem
    ADD CONSTRAINT FK_OrderItem_Menu FOREIGN KEY (MenuId) REFERENCES dbo.Menu(MenuId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_OrderItem_Target')
BEGIN
    ALTER TABLE dbo.OrderItem
    ADD CONSTRAINT CK_OrderItem_Target CHECK (
        (ProductId IS NOT NULL AND MenuId IS NULL AND ItemType = 'Preparat')
        OR
        (ProductId IS NULL AND MenuId IS NOT NULL AND ItemType = 'Meniu')
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItem_MenuId')
    CREATE INDEX IX_OrderItem_MenuId ON dbo.OrderItem(MenuId);
GO

PRINT 'OrderItem now supports products and bundled menus.';
