
using Microsoft.EntityFrameworkCore;
using StockHold.Application.Abstractions.Persistence.Products;
using StockHold.Domain.Entities;
using StockHold.Infrastructure.Persistence.Contexts;

namespace StockHold.Infrastructure.Persistence.Repositories.Products;

public sealed class ProductRepository : IProductRepository
{
    private readonly StockHoldDbContext _context;

    public ProductRepository(StockHoldDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int productId)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<Product?> GetByIdWithLockAsync(int productId)
    {
        return await _context.Products
            .FromSqlInterpolated($@"
                SELECT *
                FROM [dbo].[Products] WITH (UPDLOCK, ROWLOCK)
                WHERE [Id] = {productId}")
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }
}

