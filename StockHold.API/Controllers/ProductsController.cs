
using Microsoft.AspNetCore.Mvc;
using StockHold.API.Contracts.Products;
using StockHold.Application.Features.Products.CreateProduct;

namespace StockHold.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly CreateProductService _createProductService;

    public ProductsController(CreateProductService createProductService)
    {
        _createProductService = createProductService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request)
    {
        var product = await _createProductService.ExecuteAsync(
            request.Name,
            request.Quantity);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            new
            {
                product.Id,
                product.Name,
                product.Quantity
            });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(new { id });
    }
}
