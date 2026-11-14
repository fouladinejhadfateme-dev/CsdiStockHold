
using StockHold.Domain.Entities;

namespace StockHold.Application.Abstractions.Persistence.Products
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int productId);

        Task<Product?> GetByIdWithLockAsync(int productId);

        Task AddAsync(Product product);
    }
}
