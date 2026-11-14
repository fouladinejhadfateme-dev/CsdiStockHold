
using StockHold.Application.Abstractions.Persistence;
using StockHold.Application.Abstractions.Persistence.Products;
using StockHold.Domain.Entities;

namespace StockHold.Application.Features.Products.CreateProduct;

public sealed class CreateProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> ExecuteAsync(string name, int quantity)
    {
        var product = new Product(name, quantity);

        await _productRepository.AddAsync(product);

        await _unitOfWork.SaveChangesAsync();

        return product;
    }
}
