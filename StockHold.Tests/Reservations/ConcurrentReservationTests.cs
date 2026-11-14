
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockHold.Infrastructure.Persistence.Contexts;
using StockHold.Tests.Factories;
using Xunit;
using System.Text.Json;
namespace StockHold.Tests.Reservations;

public class ConcurrentReservationTests
{
    private const string ConnectionString =
        "Server=DESKTOP-7DRGR3B;Database=StockHoldTestDb;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public async Task Two_concurrent_reservations_should_not_oversell()
    {
        var options = new DbContextOptionsBuilder<StockHoldDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        int productId;
        int customerOneId;
        int customerTwoId;

        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync("""
                DELETE FROM dbo.Reservations;
                DELETE FROM dbo.Customers;
                DELETE FROM dbo.Products;

                DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Products', RESEED, 0);

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_Reservations_Customer_IdempotencyKey'
                      AND object_id = OBJECT_ID(N'dbo.Reservations')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Reservations_Customer_IdempotencyKey
                    ON dbo.Reservations (CustomerId, IdempotencyKey);
                END;

                INSERT INTO dbo.Products (Name, Quantity)
                VALUES (N'Concurrent Test Product', 5);

                INSERT INTO dbo.Customers (Name, CustomerType)
                VALUES
                    (N'Test Customer One', N'Special'),
                    (N'Test Customer Two', N'Special');
                """);

            productId = await db.Products
                .Where(p => p.Name == "Concurrent Test Product")
                .Select(p => p.Id)
                .SingleAsync();

            var customerIds = await db.Customers
                .Where(c => c.Name.StartsWith("Test Customer"))
                .OrderBy(c => c.Id)
                .Select(c => c.Id)
                .ToListAsync();

            customerOneId = customerIds[0];
            customerTwoId = customerIds[1];
        }

        using var factory = new StockHoldApiFactory(ConnectionString);
        using var client = factory.CreateClient();

        var requestOne = new
        {
            CustomerId = customerOneId,
            ProductId = productId,
            Quantity = 4,
            IdempotencyKey = "concurrent-test-001"
        };

        var requestTwo = new
        {
            CustomerId = customerTwoId,
            ProductId = productId,
            Quantity = 4,
            IdempotencyKey = "concurrent-test-002"
        };

        var responseTasks = new[]
        {
            client.PostAsJsonAsync("/api/Reservations", requestOne),
            client.PostAsJsonAsync("/api/Reservations", requestTwo)
        };

        var responses = await Task.WhenAll(responseTasks);

        var responseDetails = await Task.WhenAll(
            responses.Select(async response =>
            {
                var body = await response.Content.ReadAsStringAsync();

                return $"Status: {(int)response.StatusCode} " +
                       $"{response.StatusCode}\nBody: {body}";
            }));

        var successfulCount = responses.Count(
            response => response.StatusCode == HttpStatusCode.Created);

        var rejectedCount = responses.Count(
            response => response.StatusCode == HttpStatusCode.BadRequest);

        Assert.True(
            successfulCount == 1,
            $"Expected exactly one successful request, " +
            $"but got {successfulCount}.\n" +
            string.Join("\n\n", responseDetails));

        Assert.True(
            rejectedCount == 1,
            $"Expected exactly one rejected request, " +
            $"but got {rejectedCount}.\n" +
            string.Join("\n\n", responseDetails));

        await using var verificationDb = new StockHoldDbContext(options);

        var reservations = await verificationDb.Reservations
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        Assert.True(
            reservations.Count == 1,
            $"Expected 1 reservation, but found {reservations.Count}.");

        Assert.Equal(4, reservations[0].Quantity);

        var product = await verificationDb.Products
            .AsNoTracking()
            .SingleAsync(p => p.Id == productId);

        var reservedQuantity = reservations.Sum(r => r.Quantity);
        var availableQuantity = product.Quantity - reservedQuantity;

        Assert.Equal(1, availableQuantity);
    }

    [Fact]
    public async Task Repeated_request_with_same_idempotency_key_should_return_same_reservation()
    {
        var options = new DbContextOptionsBuilder<StockHoldDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        int productId;
        int customerId;

        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync("""
                DELETE FROM dbo.Reservations;
                DELETE FROM dbo.Customers;
                DELETE FROM dbo.Products;

                DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Products', RESEED, 0);

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_Reservations_Customer_IdempotencyKey'
                      AND object_id = OBJECT_ID(N'dbo.Reservations')
                )
                BEGIN
                    CREATE UNIQUE INDEX UX_Reservations_Customer_IdempotencyKey
                    ON dbo.Reservations (CustomerId, IdempotencyKey);
                END;

                INSERT INTO dbo.Products (Name, Quantity)
                VALUES (N'Idempotency Test Product', 5);

                INSERT INTO dbo.Customers (Name, CustomerType)
                VALUES (N'Idempotency Test Customer', N'Special');
                """);

            productId = await db.Products
                .Where(p => p.Name == "Idempotency Test Product")
                .Select(p => p.Id)
                .SingleAsync();

            customerId = await db.Customers
                .Where(c => c.Name == "Idempotency Test Customer")
                .Select(c => c.Id)
                .SingleAsync();
        }

        using var factory = new StockHoldApiFactory(ConnectionString);
        using var client = factory.CreateClient();

        var request = new
        {
            CustomerId = customerId,
            ProductId = productId,
            Quantity = 2,
            IdempotencyKey = "idempotency-test-001"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/Reservations",
            request);

        var firstResponseBody = await firstResponse.Content
            .ReadAsStringAsync();

        var secondResponse = await client.PostAsJsonAsync(
            "/api/Reservations",
            request);

        var secondResponseBody = await secondResponse.Content
            .ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode == HttpStatusCode.Created,
            $"First request failed.\n" +
            $"Status: {firstResponse.StatusCode}\n" +
            $"Body: {firstResponseBody}");

        Assert.True(
            secondResponse.StatusCode == HttpStatusCode.Created,
            $"Second request failed.\n" +
            $"Status: {secondResponse.StatusCode}\n" +
            $"Body: {secondResponseBody}");

        // استخراج شناسه رزرو از هر دو پاسخ JSON
        using var firstJson = JsonDocument.Parse(firstResponseBody);
        using var secondJson = JsonDocument.Parse(secondResponseBody);

        var firstReservationId =
            firstJson.RootElement.GetProperty("id").GetInt32();

        var secondReservationId =
            secondJson.RootElement.GetProperty("id").GetInt32();

        // هر دو پاسخ باید شناسه یکسان داشته باشند
        Assert.Equal(firstReservationId, secondReservationId);

        // بررسی دیتابیس
        await using var verificationDb = new StockHoldDbContext(options);

        var reservations = await verificationDb.Reservations
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .ToListAsync();

        Assert.Single(reservations);

        Assert.Equal(2, reservations[0].Quantity);

        Assert.Equal(
            "idempotency-test-001",
            reservations[0].IdempotencyKey);

        Assert.Equal(
            reservations[0].Id,
            firstReservationId);
    }

    [Fact]
    public async Task Expired_reservation_should_release_stock()
    {
        var options = new DbContextOptionsBuilder<StockHoldDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        int productId;
        int customerId;

        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM dbo.Reservations;
            DELETE FROM dbo.Customers;
            DELETE FROM dbo.Products;

            DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Products', RESEED, 0);

            INSERT INTO dbo.Products (Name, Quantity)
            VALUES (N'Expiration Test Product', 5);

            INSERT INTO dbo.Customers (Name, CustomerType)
            VALUES (N'Expiration Test Customer', N'Special');
            """);

            productId = await db.Products
                .Where(p => p.Name == "Expiration Test Product")
                .Select(p => p.Id)
                .SingleAsync();

            customerId = await db.Customers
                .Where(c => c.Name == "Expiration Test Customer")
                .Select(c => c.Id)
                .SingleAsync();
        }

        using var factory = new StockHoldApiFactory(ConnectionString);
        using var client = factory.CreateClient();

        // 1. ثبت رزرو اولیه به مقدار 2
        var firstRequest = new
        {
            CustomerId = customerId,
            ProductId = productId,
            Quantity = 2,
            IdempotencyKey = "expiration-test-001"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/Reservations",
            firstRequest);

        var firstResponseBody = await firstResponse.Content
            .ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode == HttpStatusCode.Created,
            $"Reservation creation failed.\n{firstResponseBody}");

        using var firstJson = JsonDocument.Parse(firstResponseBody);

        var reservationId =
            firstJson.RootElement.GetProperty("id").GetInt32();

        // 2. تغییر زمان انقضا به گذشته
        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE dbo.Reservations
            SET ExpiresAt = DATEADD(MINUTE, -1, SYSUTCDATETIME())
            WHERE Id = {reservationId};
            """);
        }

        // 3. اجرای API انقضا
        var expireResponse = await client.PostAsync(
            "/api/Reservations/expire",
            null);

        var expireResponseBody = await expireResponse.Content
            .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            expireResponse.StatusCode);

        using var expireJson = JsonDocument.Parse(expireResponseBody);

        var expiredCount = expireJson.RootElement
            .GetProperty("expiredCount")
            .GetInt32();

        Assert.Equal(1, expiredCount);

        // 4. بررسی اینکه رزرو قبلی Expired شده است
        await using var verificationDb =
            new StockHoldDbContext(options);

        var expiredReservation = await verificationDb.Reservations
            .AsNoTracking()
            .SingleAsync(r => r.Id == reservationId);

        Assert.Equal("Expired", expiredReservation.Status.ToString());

        // 5. تلاش برای رزرو کل موجودی کالا
        // چون رزرو قبلی منقضی شده، باید 5 واحد قابل رزرو باشد.
        var secondRequest = new
        {
            CustomerId = customerId,
            ProductId = productId,
            Quantity = 5,
            IdempotencyKey = "expiration-test-002"
        };

        var secondResponse = await client.PostAsJsonAsync(
            "/api/Reservations",
            secondRequest);

        var secondResponseBody = await secondResponse.Content
            .ReadAsStringAsync();

        Assert.True(
            secondResponse.StatusCode == HttpStatusCode.Created,
            $"Stock was not released after expiration.\n" +
            $"Status: {secondResponse.StatusCode}\n" +
            $"Body: {secondResponseBody}");
    }
    [Fact]
    public async Task Normal_customer_should_be_allowed_to_reserve_3_units()
    {
        await AssertReservationLimitAsync(
            customerType: "Normal",
            requestedQuantity: 3,
            shouldSucceed: true);
    }

    [Fact]
    public async Task Normal_customer_should_not_be_allowed_to_reserve_4_units()
    {
        await AssertReservationLimitAsync(
            customerType: "Normal",
            requestedQuantity: 4,
            shouldSucceed: false);
    }

    [Fact]
    public async Task Special_customer_should_be_allowed_to_reserve_10_units()
    {
        await AssertReservationLimitAsync(
            customerType: "Special",
            requestedQuantity: 10,
            shouldSucceed: true);
    }

    [Fact]
    public async Task Special_customer_should_not_be_allowed_to_reserve_11_units()
    {
        await AssertReservationLimitAsync(
            customerType: "Special",
            requestedQuantity: 11,
            shouldSucceed: false);
    }

    private async Task AssertReservationLimitAsync(
        string customerType,
        int requestedQuantity,
        bool shouldSucceed)
    {
        var options = new DbContextOptionsBuilder<StockHoldDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        int productId;
        int customerId;

        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM dbo.Reservations;
            DELETE FROM dbo.Customers;
            DELETE FROM dbo.Products;

            DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Products', RESEED, 0);

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'UX_Reservations_Customer_IdempotencyKey'
                  AND object_id = OBJECT_ID(N'dbo.Reservations')
            )
            BEGIN
                CREATE UNIQUE INDEX UX_Reservations_Customer_IdempotencyKey
                ON dbo.Reservations (CustomerId, IdempotencyKey);
            END;

            INSERT INTO dbo.Products (Name, Quantity)
            VALUES (N'Customer Limit Test Product', 20);
            """);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.Customers (Name, CustomerType)
            VALUES (N'Customer Limit Test Customer', {customerType});
            """);

            productId = await db.Products
                .Where(p => p.Name == "Customer Limit Test Product")
                .Select(p => p.Id)
                .SingleAsync();

            customerId = await db.Customers
                .Where(c => c.Name == "Customer Limit Test Customer")
                .Select(c => c.Id)
                .SingleAsync();
        }

        using var factory = new StockHoldApiFactory(ConnectionString);
        using var client = factory.CreateClient();

        var request = new
        {
            CustomerId = customerId,
            ProductId = productId,
            Quantity = requestedQuantity,
            IdempotencyKey = $"limit-test-{customerType}-{requestedQuantity}"
        };

        var response = await client.PostAsJsonAsync(
            "/api/Reservations",
            request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (shouldSucceed)
        {
            Assert.True(
                response.StatusCode == HttpStatusCode.Created,
                $"Expected Created, but got {response.StatusCode}.\n" +
                $"Response: {responseBody}");
        }
        else
        {
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected BadRequest, but got {response.StatusCode}.\n" +
                $"Response: {responseBody}");
        }

        await using var verificationDb = new StockHoldDbContext(options);

        var reservations = await verificationDb.Reservations
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .ToListAsync();

        if (shouldSucceed)
        {
            Assert.Single(reservations);
            Assert.Equal(requestedQuantity, reservations[0].Quantity);
        }
        else
        {
            Assert.Empty(reservations);
        }
    }

    [Fact]
    public async Task Create_product_should_save_product_in_database()
    {
        var options = new DbContextOptionsBuilder<StockHoldDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using (var db = new StockHoldDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM dbo.Reservations;
            DELETE FROM dbo.Customers;
            DELETE FROM dbo.Products;

            DBCC CHECKIDENT ('dbo.Reservations', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
            DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
            """);
        }

        using var factory = new StockHoldApiFactory(ConnectionString);
        using var client = factory.CreateClient();

        var request = new
        {
            Name = "Unit Test Laptop",
            Quantity = 20
        };

        var response = await client.PostAsJsonAsync(
            "/api/Products",
            request);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected Created, but got {response.StatusCode}.\n" +
            $"Response: {responseBody}");

        using var json = JsonDocument.Parse(responseBody);

        var productId = json.RootElement
            .GetProperty("id")
            .GetInt32();

        Assert.Equal(
            "Unit Test Laptop",
            json.RootElement.GetProperty("name").GetString());

        Assert.Equal(
            20,
            json.RootElement.GetProperty("quantity").GetInt32());

        await using var verificationDb = new StockHoldDbContext(options);

        var product = await verificationDb.Products
            .AsNoTracking()
            .SingleAsync(p => p.Id == productId);

        Assert.Equal("Unit Test Laptop", product.Name);
        Assert.Equal(20, product.Quantity);
    }

}
