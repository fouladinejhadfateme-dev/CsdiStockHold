
using Microsoft.EntityFrameworkCore;

using StockHold.API.BackgroundServices;
using StockHold.API.Middleware;

using StockHold.Application.Abstractions.Persistence;
using StockHold.Application.Abstractions.Persistence.Customers;
using StockHold.Application.Abstractions.Persistence.Products;
using StockHold.Application.Abstractions.Persistence.Reservations;

using StockHold.Application.Features.Reservations.CreateReservation;
using StockHold.Application.Features.Reservations.ExpireReservations;

using StockHold.Infrastructure.Persistence.Contexts;
using StockHold.Infrastructure.Persistence.Repositories.Customers;
using StockHold.Infrastructure.Persistence.Repositories.Products;
using StockHold.Infrastructure.Persistence.Repositories.Reservations;
using StockHold.Infrastructure.Persistence.UnitOfWork;
using StockHold.Application.Features.Products.CreateProduct;

var builder = WebApplication.CreateBuilder(args);

// 1. اتصال به دیتابیس SQL Server
builder.Services.AddDbContext<StockHoldDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("StockHoldConnection")));

// 2. ثبت Repositoryها
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();


// 3. ثبت UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. ثبت سرویس‌های Application
builder.Services.AddScoped<CreateReservationService>();
builder.Services.AddScoped<ExpireReservationsService>();
builder.Services.AddScoped<CreateProductService>();

// 5. ثبت BackgroundService
builder.Services.AddHostedService<ReservationExpirationBackgroundService>();

// 6. تنظیمات API و Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. مدیریت خطاهای برنامه
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 8. تنظیم Swagger در محیط Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 9. تنظیمات HTTP Pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
public partial class Program { }