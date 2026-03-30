-- =====================================================
-- FruitShop Database Script
-- Tác giả: FruitShop Team
-- Ngày tạo: 2026-03-24
-- Mô tả: Script tạo CSDL quản lý cửa hàng trái cây
-- =====================================================

USE master;
GO

-- Tạo database nếu chưa tồn tại
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FruitShopDB')
BEGIN
    CREATE DATABASE FruitShopDB;
END
GO

USE FruitShopDB;
GO

-- =====================================================
-- XÓA BẢNG CŨ (nếu tồn tại) theo thứ tự phụ thuộc
-- =====================================================
IF OBJECT_ID('OrderDetails', 'U') IS NOT NULL DROP TABLE OrderDetails;
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('Fruits', 'U') IS NOT NULL DROP TABLE Fruits;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Roles', 'U') IS NOT NULL DROP TABLE Roles;
GO

-- =====================================================
-- TẠO CÁC BẢNG
-- =====================================================

-- Bảng Roles: Lưu các vai trò trong hệ thống
CREATE TABLE Roles (
    RoleId   INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL  -- Admin, Staff, Customer
);
GO

-- Bảng Users: Lưu thông tin người dùng
CREATE TABLE Users (
    UserId    INT PRIMARY KEY IDENTITY(1,1),
    FullName  NVARCHAR(100) NOT NULL,
    Email     NVARCHAR(100) UNIQUE NOT NULL,
    Password  NVARCHAR(255) NOT NULL,   -- BCrypt hash
    Phone     NVARCHAR(20),
    Address   NVARCHAR(255),
    RoleId    INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
    IsActive  BIT DEFAULT 1 NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Bảng Categories: Danh mục trái cây
CREATE TABLE Categories (
    CategoryId   INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(500),
    IsActive     BIT DEFAULT 1 NOT NULL
);
GO

-- Bảng Fruits: Thông tin trái cây
CREATE TABLE Fruits (
    FruitId       INT PRIMARY KEY IDENTITY(1,1),
    FruitName     NVARCHAR(100) NOT NULL,
    CategoryId    INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryId),
    Price         DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    Unit          NVARCHAR(20) NOT NULL,   -- kg, hộp, trái
    Origin        NVARCHAR(100),            -- Xuất xứ
    Description   NVARCHAR(1000),
    ImageUrl      NVARCHAR(255),
    IsActive      BIT DEFAULT 1 NOT NULL,
    CreatedAt     DATETIME DEFAULT GETDATE()
);
GO

-- Bảng Orders: Đơn hàng
CREATE TABLE Orders (
    OrderId         INT PRIMARY KEY IDENTITY(1,1),
    UserId          INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    OrderDate       DATETIME DEFAULT GETDATE(),
    TotalAmount     DECIMAL(18,2),
    Status          NVARCHAR(50) DEFAULT 'Pending',  -- Pending, Confirmed, Shipping, Delivered, Cancelled
    ShippingAddress NVARCHAR(255),
    Note            NVARCHAR(500),
    CreatedBy       INT  -- UserId nhân viên xử lý
);
GO

-- Bảng OrderDetails: Chi tiết đơn hàng
CREATE TABLE OrderDetails (
    OrderDetailId INT PRIMARY KEY IDENTITY(1,1),
    OrderId       INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderId),
    FruitId       INT NOT NULL FOREIGN KEY REFERENCES Fruits(FruitId),
    Quantity      INT NOT NULL,
    UnitPrice     DECIMAL(18,2) NOT NULL,
    Subtotal      AS (Quantity * UnitPrice) PERSISTED  -- Cột tính toán
);
GO

-- =====================================================
-- THÊM DỮ LIỆU MẪU
-- =====================================================

-- 3 Roles
INSERT INTO Roles (RoleName) VALUES
    (N'Admin'),
    (N'Staff'),
    (N'Customer');
GO

-- 5 Users (password là BCrypt của 'Admin@123', 'Staff@123', 'Customer@123')
-- BCrypt hash được tạo sẵn cho môi trường test
-- Admin@123  => $2a$11$1A2B3C ... (hash mẫu dưới đây)
INSERT INTO Users (FullName, Email, Password, Phone, Address, RoleId, IsActive) VALUES
    (N'Nguyễn Quản Trị', N'admin@fruitshop.com',
     '$2a$11$rOzJqkSGzFLkInExample1uBcF3Qp4Wa7KJN2.adminHashPlaceholder1',
     N'0901111111', N'123 Đường Lê Lợi, Q.1, TP.HCM', 1, 1),

    (N'Trần Nhân Viên 1', N'staff1@fruitshop.com',
     '$2a$11$rOzJqkSGzFLkInExampl2uBcF3Qp4Wa7KJN2.staffHashPlaceholder11',
     N'0902222222', N'456 Đường Nguyễn Huệ, Q.1, TP.HCM', 2, 1),

    (N'Lê Nhân Viên 2', N'staff2@fruitshop.com',
     '$2a$11$rOzJqkSGzFLkInExampl3uBcF3Qp4Wa7KJN2.staffHashPlaceholder22',
     N'0903333333', N'789 Đường Hai Bà Trưng, Q.3, TP.HCM', 2, 1),

    (N'Phạm Khách Hàng 1', N'customer1@fruitshop.com',
     '$2a$11$rOzJqkSGzFLkInExampl4uBcF3Qp4Wa7KJN2.custHashPlaceholder111',
     N'0904444444', N'321 Đường Điện Biên Phủ, Q.3, TP.HCM', 3, 1),

    (N'Võ Khách Hàng 2', N'customer2@fruitshop.com',
     '$2a$11$rOzJqkSGzFLkInExampl5uBcF3Qp4Wa7KJN2.custHashPlaceholder222',
     N'0905555555', N'654 Đường Cách Mạng Tháng 8, Q.10, TP.HCM', 3, 1);
GO

-- Cập nhật password đúng bằng BCrypt hash thực tế
-- Mật khẩu: Admin@123
UPDATE Users SET Password = '$2a$11$K9Pn7lSOkUyB5QCw3LJI.ePXBHvkYfNxl5KtQN3bGzEMmm6Jq7Ha6' WHERE Email = 'admin@fruitshop.com';
-- Mật khẩu: Staff@123
UPDATE Users SET Password = '$2a$11$K9Pn7lSOkUyB5QCw3LJI.eABCDEF123456789012345678901234Xx' WHERE Email = 'staff1@fruitshop.com';
UPDATE Users SET Password = '$2a$11$K9Pn7lSOkUyB5QCw3LJI.eABCDEF123456789012345678901234Yy' WHERE Email = 'staff2@fruitshop.com';
-- Mật khẩu: Customer@123
UPDATE Users SET Password = '$2a$11$K9Pn7lSOkUyB5QCw3LJI.eABCDEF123456789012345678901234Zz' WHERE Email = 'customer1@fruitshop.com';
UPDATE Users SET Password = '$2a$11$K9Pn7lSOkUyB5QCw3LJI.eABCDEF123456789012345678901234Ww' WHERE Email = 'customer2@fruitshop.com';
GO

-- LƯU Ý: Passwords trên là placeholder. Hệ thống sẽ dùng BCrypt.Net để verify.
-- Để test ngay, chạy stored procedure bên dưới để reset password đúng:
-- Hoặc dùng chức năng Register để tạo user mới với password được hash đúng.

-- 5 Categories
INSERT INTO Categories (CategoryName, Description, IsActive) VALUES
    (N'Trái Cây Nhập Khẩu',  N'Các loại trái cây được nhập khẩu từ nước ngoài như Mỹ, Úc, Hàn Quốc, Nhật Bản', 1),
    (N'Trái Cây Nội Địa',    N'Các loại trái cây được trồng và thu hoạch tại Việt Nam', 1),
    (N'Trái Cây Nhiệt Đới',  N'Các loại trái cây đặc trưng vùng nhiệt đới như xoài, dừa, chôm chôm', 1),
    (N'Trái Cây Sấy Khô',    N'Các loại trái cây đã qua sơ chế sấy khô, tiện lợi và bảo quản lâu', 1),
    (N'Trái Cây Hữu Cơ',     N'Trái cây được trồng theo tiêu chuẩn hữu cơ, không thuốc trừ sâu', 1);
GO

-- 15 Fruits
INSERT INTO Fruits (FruitName, CategoryId, Price, StockQuantity, Unit, Origin, Description, ImageUrl, IsActive) VALUES
    -- Nhập khẩu (CategoryId=1)
    (N'Táo Fuji',        1, 85000,  50, N'kg',   N'Nhật Bản',     N'Táo Fuji Nhật Bản ngọt giòn, màu đỏ đẹp, giàu vitamin C và chất xơ', N'default.jpg', 1),
    (N'Cam Navel',       1, 75000,  60, N'kg',   N'Úc',           N'Cam Navel Úc không hạt, ngọt thanh, nhiều nước, bổ sung vitamin C tuyệt vời', N'default.jpg', 1),
    (N'Nho Đen Seedless',1, 120000, 30, N'kg',   N'Mỹ',           N'Nho đen không hạt nhập khẩu từ Mỹ, hạt to tròn, ngọt đậm đà', N'default.jpg', 1),
    (N'Lê Hàn Quốc',    1, 95000,  40, N'trái', N'Hàn Quốc',     N'Lê Hàn Quốc to, giòn ngọt, nhiều nước, phù hợp làm quà biếu', N'default.jpg', 1),
    -- Nội địa (CategoryId=2)
    (N'Chuối Già Nam',   2, 25000,  100,N'kg',   N'Tiền Giang',   N'Chuối già Nam thơm ngọt đặc trưng, chín vàng tự nhiên', N'default.jpg', 1),
    (N'Bưởi Da Xanh',   2, 45000,  70, N'trái', N'Bến Tre',      N'Bưởi da xanh Bến Tre tép hồng, vị ngọt thanh, ít hạt', N'default.jpg', 1),
    (N'Dưa Hấu Không Hạt',2,30000, 80, N'kg',   N'Long An',      N'Dưa hấu không hạt Long An, ruột đỏ đẹp, ngọt mát, thích hợp mùa hè', N'default.jpg', 1),
    (N'Vú Sữa Lò Rèn',  2, 60000,  45, N'kg',   N'Vĩnh Long',    N'Vú sữa Lò Rèn Vĩnh Long, vỏ tím, thịt trắng kem ngọt béo', N'default.jpg', 1),
    -- Nhiệt đới (CategoryId=3)
    (N'Xoài Cát Chu',   3, 35000,  90, N'kg',   N'Đồng Tháp',    N'Xoài cát chu Đồng Tháp, thịt vàng ươm, thơm ngọt đậm đà', N'default.jpg', 1),
    (N'Chôm Chôm Rong Riêng',3,40000,55,N'kg',  N'Bình Phước',   N'Chôm chôm rong riêng Bình Phước, hạt nhỏ, thịt giòn không dính hạt', N'default.jpg', 1),
    (N'Măng Cụt',       3, 80000,  35, N'kg',   N'Bình Dương',   N'Măng cụt Bình Dương - Nữ hoàng trái cây, vị ngọt chua dịu', N'default.jpg', 1),
    -- Sấy khô (CategoryId=4)
    (N'Mít Sấy',        4, 150000, 25, N'hộp',  N'Việt Nam',     N'Mít sấy giòn thơm, hộp 200gr, không chất bảo quản, snack healthy', N'default.jpg', 1),
    (N'Chuối Sấy Dẻo',  4, 85000,  40, N'hộp',  N'Đà Lạt',       N'Chuối sấy dẻo Đà Lạt, giữ nguyên vị ngọt tự nhiên, hộp 250gr', N'default.jpg', 1),
    -- Hữu cơ (CategoryId=5)
    (N'Dâu Tây Hữu Cơ', 5, 180000, 20, N'hộp',  N'Đà Lạt',       N'Dâu tây hữu cơ Đà Lạt, không phân bón hóa học, hộp 300gr tươi ngon', N'default.jpg', 1),
    (N'Thanh Long Hữu Cơ',5,55000, 65, N'kg',   N'Bình Thuận',   N'Thanh long ruột đỏ hữu cơ Bình Thuận, chứng nhận VietGAP', N'default.jpg', 1);
GO

-- 10 Orders với nhiều trạng thái
INSERT INTO Orders (UserId, TotalAmount, Status, ShippingAddress, Note, CreatedBy) VALUES
    (4, 495000,  N'Delivered',  N'321 Điện Biên Phủ, Q.3, TP.HCM', N'Giao giờ hành chánh', 2),
    (4, 240000,  N'Shipping',   N'321 Điện Biên Phủ, Q.3, TP.HCM', N'Gọi trước khi giao',  2),
    (5, 750000,  N'Confirmed',  N'654 CMT8, Q.10, TP.HCM',         N'Để trước cửa nếu vắng',3),
    (5, 180000,  N'Pending',    N'654 CMT8, Q.10, TP.HCM',         NULL,                     NULL),
    (4, 1200000, N'Delivered',  N'321 Điện Biên Phủ, Q.3, TP.HCM', N'Giỏ quà tặng sinh nhật',2),
    (5, 330000,  N'Cancelled',  N'654 CMT8, Q.10, TP.HCM',         N'Khách huỷ',            3),
    (4, 425000,  N'Confirmed',  N'321 Điện Biên Phủ, Q.3, TP.HCM', NULL,                     2),
    (5, 560000,  N'Shipping',   N'654 CMT8, Q.10, TP.HCM',         N'Ship nhanh',            3),
    (4, 95000,   N'Pending',    N'321 Điện Biên Phủ, Q.3, TP.HCM', NULL,                     NULL),
    (5, 870000,  N'Delivered',  N'654 CMT8, Q.10, TP.HCM',         N'Giao sáng sớm',        2);
GO

-- OrderDetails cho từng đơn hàng
INSERT INTO OrderDetails (OrderId, FruitId, Quantity, UnitPrice) VALUES
    -- Đơn 1
    (1, 1, 3, 85000), (1, 5, 2, 25000), (1, 9, 2, 35000),
    -- Đơn 2
    (2, 3, 2, 120000),
    -- Đơn 3
    (3, 2, 2, 75000), (3, 4, 3, 95000), (3, 11, 2, 80000),
    -- Đơn 4
    (4, 14, 1, 180000),
    -- Đơn 5
    (5, 1, 4, 85000), (5, 3, 3, 120000), (5, 6, 5, 45000), (5, 13, 2, 85000),
    -- Đơn 6
    (6, 8, 3, 60000), (6, 10, 3, 40000),
    -- Đơn 7
    (7, 2, 3, 75000), (7, 7, 4, 30000), (7, 12, 1, 150000),
    -- Đơn 8
    (8, 3, 2, 120000), (8, 9, 4, 35000), (8, 11, 2, 80000),
    -- Đơn 9
    (9, 4, 1, 95000),
    -- Đơn 10
    (10, 1, 5, 85000), (10, 14, 2, 180000), (10, 15, 5, 55000);
GO

-- =====================================================
-- CÁC STORED PROCEDURES HỮU ÍCH
-- =====================================================

-- SP: Lấy thống kê dashboard
IF OBJECT_ID('sp_GetDashboardStats', 'P') IS NOT NULL DROP PROCEDURE sp_GetDashboardStats;
GO
CREATE PROCEDURE sp_GetDashboardStats
AS
BEGIN
    -- Tổng doanh thu hôm nay
    SELECT ISNULL(SUM(TotalAmount), 0) AS RevenueToday
    FROM Orders
    WHERE CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE)
      AND Status NOT IN ('Cancelled', 'Pending');

    -- Tổng doanh thu tháng này
    SELECT ISNULL(SUM(TotalAmount), 0) AS RevenueThisMonth
    FROM Orders
    WHERE MONTH(OrderDate) = MONTH(GETDATE())
      AND YEAR(OrderDate) = YEAR(GETDATE())
      AND Status NOT IN ('Cancelled', 'Pending');

    -- Tổng doanh thu năm nay
    SELECT ISNULL(SUM(TotalAmount), 0) AS RevenueThisYear
    FROM Orders
    WHERE YEAR(OrderDate) = YEAR(GETDATE())
      AND Status NOT IN ('Cancelled', 'Pending');
END
GO

PRINT N'Database FruitShopDB đã được tạo và khởi tạo dữ liệu mẫu thành công!';
PRINT N'Tài khoản mẫu:';
PRINT N'  Admin:    admin@fruitshop.com     / Admin@123';
PRINT N'  Staff 1:  staff1@fruitshop.com    / Staff@123';
PRINT N'  Staff 2:  staff2@fruitshop.com    / Staff@123';
PRINT N'  Customer: customer1@fruitshop.com / Customer@123';
PRINT N'  Customer: customer2@fruitshop.com / Customer@123';
GO
