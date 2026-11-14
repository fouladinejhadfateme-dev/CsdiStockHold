
IF DB_ID('StockHoldTestDb') IS NULL
BEGIN
    CREATE DATABASE StockHoldTestDb;
END
GO

USE StockHoldTestDb;
GO

-- جدول Products
IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL
            CONSTRAINT CK_Products_Quantity CHECK (Quantity >= 0)
    );
END
GO

-- جدول Customers
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        CustomerType NVARCHAR(20) NOT NULL
            CONSTRAINT CK_Customers_CustomerType
            CHECK (CustomerType IN ('Normal', 'Special'))
    );
END
GO

-- جدول Reservations
IF OBJECT_ID('dbo.Reservations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservations
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        CustomerId INT NOT NULL,
        ProductId INT NOT NULL,

        Quantity INT NOT NULL
            CONSTRAINT CK_Reservations_Quantity CHECK (Quantity > 0),

        CreatedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,

        Status NVARCHAR(20) NOT NULL
            CONSTRAINT CK_Reservations_Status
            CHECK (Status IN ('Active', 'Expired', 'Cancelled')),

        IdempotencyKey NVARCHAR(100) NOT NULL,

        CONSTRAINT FK_Reservations_Customers
            FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(Id),

        CONSTRAINT FK_Reservations_Products
            FOREIGN KEY (ProductId)
            REFERENCES dbo.Products(Id)
    );
END
GO

-- جلوگیری از ثبت رزرو تکراری برای یک مشتری و یک کلید
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Reservations_Customer_IdempotencyKey'
      AND object_id = OBJECT_ID('dbo.Reservations')
)
BEGIN
    CREATE UNIQUE INDEX UX_Reservations_Customer_IdempotencyKey
    ON dbo.Reservations(CustomerId, IdempotencyKey);
END
GO

-- ایندکس برای جستجوی رزروهای فعال و منقضی‌شده
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Reservations_Product_Status_ExpiresAt'
      AND object_id = OBJECT_ID('dbo.Reservations')
)
BEGIN
    CREATE INDEX IX_Reservations_Product_Status_ExpiresAt
    ON dbo.Reservations(ProductId, Status, ExpiresAt);
END
GO
