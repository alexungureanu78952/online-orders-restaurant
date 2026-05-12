-- =====================================================
-- Demo users for local testing
-- =====================================================

USE RestaurantOrderManagement;
GO

SET NOCOUNT ON;

-- Password: employee123
-- Hashing matches AuthenticationService SHA256 + Base64 implementation.
MERGE dbo.[User] AS target
USING (VALUES
    ('employee@restaurant.local',
     'Wy+OJ+LltAgcA85wsojIe9EmMUDL0b2a4HgSNQm3yv8=',
     'Demo',
     'Employee',
     '0700000000',
     'Restaurant back office',
     'Employee')
) AS source (Email, PasswordHash, FirstName, LastName, PhoneNumber, DeliveryAddress, [Role])
ON target.Email = source.Email
WHEN MATCHED THEN
    UPDATE SET PasswordHash = source.PasswordHash,
               FirstName = source.FirstName,
               LastName = source.LastName,
               PhoneNumber = source.PhoneNumber,
               DeliveryAddress = source.DeliveryAddress,
               [Role] = source.[Role],
               IsActive = 1,
               ModifiedDate = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Email, PasswordHash, FirstName, LastName, PhoneNumber, DeliveryAddress, [Role], IsActive)
    VALUES (source.Email, source.PasswordHash, source.FirstName, source.LastName, source.PhoneNumber, source.DeliveryAddress, source.[Role], 1);

PRINT 'Demo employee user is ready.';

SELECT Email, FirstName, LastName, [Role], IsActive
FROM dbo.[User]
WHERE Email = 'employee@restaurant.local';
GO
