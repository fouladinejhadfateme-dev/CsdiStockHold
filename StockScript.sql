
IF DB_ID(N'StockHoldDb') IS NULL
BEGIN
    CREATE DATABASE StockHoldDb;
END
GO

USE StockHoldDb;
GO

CREATE TABLE Products
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,

    CONSTRAINT CK_Products_StockQuantity
        CHECK (Quantity >= 0)
);
GO

CREATE TABLE Customers
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    CustomerType NVARCHAR(20) NOT NULL,

    CONSTRAINT CK_Customers_CustomerType
        CHECK (CustomerType IN ('Normal', 'Special'))
);
GO

CREATE TABLE Reservations
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    IdempotencyKey NVARCHAR(100) NOT NULL,

    CONSTRAINT CK_Reservations_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_Reservations_Status
        CHECK (Status IN ('Active', 'Expired', 'Cancelled')),

    CONSTRAINT FK_Reservations_Customers
        FOREIGN KEY (CustomerId)
        REFERENCES Customers(Id),

    CONSTRAINT FK_Reservations_Products
        FOREIGN KEY (ProductId)
        REFERENCES Products(Id)
);
GO

CREATE UNIQUE INDEX UX_Reservations_Customer_IdempotencyKey
ON Reservations(CustomerId, IdempotencyKey);
GO

CREATE INDEX IX_Reservations_Product_Status_ExpiresAt
ON Reservations(ProductId, Status, ExpiresAt);
GO
----------------------------------------------------------------------- data:-----------------------

USE StockHoldDb;
GO

/* ==========================================
   1. درج کالاهای نمونه
   ========================================== */

INSERT INTO Products (Name, Quantity)
VALUES
    (N'لپ‌تاپ Lenovo ThinkPad', 20),
    (N'گوشی موبایل Samsung Galaxy', 15),
    (N'مانیتور LG 27 Inch', 10),
    (N'کیبورد مکانیکی', 5),
    (N'ماوس Logitech', 30);
GO


/* ==========================================
   2. درج مشتریان نمونه
   ========================================== */

INSERT INTO Customers (Name, CustomerType)
VALUES
    (N'علی رضایی - مشتری عادی', 'Normal'),
    (N'مریم احمدی - مشتری ویژه', 'Special'),
    (N'محمد کریمی - مشتری عادی', 'Normal'),
    (N'سارا محمدی - مشتری ویژه', 'Special');
GO


/* ==========================================
   3. درج رزروهای نمونه
   ========================================== */

INSERT INTO Reservations
(
    CustomerId,
    ProductId,
    Quantity,
    CreatedAt,
    ExpiresAt,
    Status,
    IdempotencyKey
)
VALUES

-- رزرو فعال مشتری عادی برای لپ‌تاپ
(
    1,
    1,
    2,
    DATEADD(MINUTE, -2, SYSUTCDATETIME()),
    DATEADD(MINUTE, 8, SYSUTCDATETIME()),
    'Active',
    N'normal-laptop-001'
),

-- رزرو فعال مشتری ویژه برای گوشی
(
    2,
    2,
    5,
    DATEADD(MINUTE, -5, SYSUTCDATETIME()),
    DATEADD(MINUTE, 25, SYSUTCDATETIME()),
    'Active',
    N'special-mobile-001'
),

-- رزرو فعال مشتری عادی برای مانیتور
(
    3,
    3,
    3,
    DATEADD(MINUTE, -1, SYSUTCDATETIME()),
    DATEADD(MINUTE, 9, SYSUTCDATETIME()),
    'Active',
    N'normal-monitor-001'
),

-- رزرو منقضی‌شده مشتری ویژه
(
    4,
    4,
    4,
    DATEADD(MINUTE, -40, SYSUTCDATETIME()),
    DATEADD(MINUTE, -10, SYSUTCDATETIME()),
    'Expired',
    N'special-keyboard-001'
),

-- رزرو لغوشده مشتری عادی
(
    1,
    5,
    1,
    DATEADD(MINUTE, -3, SYSUTCDATETIME()),
    DATEADD(MINUTE, 7, SYSUTCDATETIME()),
    'Cancelled',
    N'normal-mouse-001'
);
GO


/* ==========================================
   4. مشاهده داده‌ها
   ========================================== */

SELECT * FROM Products;
GO

SELECT * FROM Customers;
GO

SELECT * FROM Reservations;
GO
