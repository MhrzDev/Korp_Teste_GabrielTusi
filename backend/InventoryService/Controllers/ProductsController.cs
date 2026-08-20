using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Code)
            .Select(product => new ProductResponse(
                product.Id,
                product.Code,
                product.Description,
                product.Stock,
                product.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Products.AnyAsync(product => product.Code == normalizedCode, cancellationToken))
            return Conflict(new { message = "A product with this code already exists." });

        var product = new Product
        {
            Code = normalizedCode,
            Description = request.Description.Trim(),
            Stock = request.Stock
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();

        product.Description = request.Description.Trim();
        product.Stock = request.Stock;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Code, product.Description, product.Stock, product.UpdatedAtUtc);
}
