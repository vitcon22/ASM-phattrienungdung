USE FruitShopDB;
GO

-- 1. Reviews table
IF OBJECT_ID('Reviews', 'U') IS NULL
BEGIN
    CREATE TABLE Reviews (
        ReviewId INT PRIMARY KEY IDENTITY(1,1),
        FruitId INT NOT NULL FOREIGN KEY REFERENCES Fruits(FruitId),
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Comment NVARCHAR(1000),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- 2. Coupons table
IF OBJECT_ID('Coupons', 'U') IS NULL
BEGIN
    CREATE TABLE Coupons (
        CouponId INT PRIMARY KEY IDENTITY(1,1),
        Code NVARCHAR(50) UNIQUE NOT NULL,
        DiscountPercent INT NOT NULL CHECK (DiscountPercent > 0 AND DiscountPercent <= 100),
        ExpiryDate DATETIME NOT NULL,
        IsActive BIT DEFAULT 1 NOT NULL
    );
END
GO

-- 3. Modify Orders table for Coupon support
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CouponId' AND Object_ID = Object_ID(N'Orders'))
BEGIN
    ALTER TABLE Orders ADD CouponId INT NULL FOREIGN KEY REFERENCES Coupons(CouponId);
    ALTER TABLE Orders ADD DiscountAmount DECIMAL(18,2) DEFAULT 0;
END
GO

-- 4. Wishlists table
IF OBJECT_ID('Wishlists', 'U') IS NULL
BEGIN
    CREATE TABLE Wishlists (
        UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        FruitId INT NOT NULL FOREIGN KEY REFERENCES Fruits(FruitId),
        AddedAt DATETIME DEFAULT GETDATE(),
        PRIMARY KEY (UserId, FruitId)
    );
END
GO

-- 5. InventoryLogs table
IF OBJECT_ID('InventoryLogs', 'U') IS NULL
BEGIN
    CREATE TABLE InventoryLogs (
        LogId INT PRIMARY KEY IDENTITY(1,1),
        FruitId INT NOT NULL FOREIGN KEY REFERENCES Fruits(FruitId),
        StaffId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        QuantityChange INT NOT NULL,
        Reason NVARCHAR(500),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Insert sample coupons
IF NOT EXISTS (SELECT * FROM Coupons)
BEGIN
    INSERT INTO Coupons (Code, DiscountPercent, ExpiryDate, IsActive) VALUES
    ('WELCOME10', 10, DATEADD(month, 1, GETDATE()), 1),
    ('SUMMER20', 20, DATEADD(month, 2, GETDATE()), 1);
END
GO
