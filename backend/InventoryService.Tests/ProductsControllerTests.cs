using InventoryService.Controllers;
using InventoryService.Data;
using InventoryService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryService.Tests;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task Create_WithValidProduct_ReturnsCreatedProduct()
    {
        await using var database = CreateDatabase();
        var controller = new ProductsController(database);

        var response = await controller.Create(
            new CreateProductRequest("prod-001", "Notebook", 10),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        var product = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal("PROD-001", product.Code);
        Assert.Equal(10, product.Stock);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ReturnsConflict()
    {
        await using var database = CreateDatabase();
        var controller = new ProductsController(database);

        await controller.Create(new CreateProductRequest("SKU-1", "First", 2), CancellationToken.None);
        var duplicate = await controller.Create(
            new CreateProductRequest("sku-1", "Duplicate", 4),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(duplicate.Result);
    }

    private static InventoryDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InventoryDbContext(options);
    }
}

