using System.Data;
using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public sealed class StockService(InventoryDbContext dbContext, ILogger<StockService> logger)
{
    public async Task<StockCommandResponse> ReserveAsync(StockCommandRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(request, "reserve", -1, cancellationToken);
    }

    public async Task<StockCommandResponse> ReleaseAsync(StockCommandRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(request, "release", 1, cancellationToken);
    }

    private async Task<StockCommandResponse> ExecuteAsync(
        StockCommandRequest request,
        string operationType,
        int direction,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("At least one stock item is required.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var alreadyProcessed = await dbContext.StockOperations.AnyAsync(
            operation => operation.IdempotencyKey == request.IdempotencyKey && operation.Type == operationType,
            cancellationToken);

        if (alreadyProcessed)
        {
            await transaction.CommitAsync(cancellationToken);
            return new StockCommandResponse(true, $"Stock {operationType} was already processed.");
        }

        var consolidatedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new StockLineRequest(group.Key, group.Sum(item => item.Quantity)))
            .OrderBy(item => item.ProductId)
            .ToArray();

        foreach (var item in consolidatedItems)
        {
            var product = await dbContext.Products
                .FromSqlInterpolated($"SELECT * FROM \"Products\" WHERE \"Id\" = {item.ProductId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Product {item.ProductId} was not found.");

            if (direction < 0 && product.Stock < item.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {product.Code}. Available: {product.Stock}; requested: {item.Quantity}.");

            product.Stock += direction * item.Quantity;
            product.UpdatedAtUtc = DateTime.UtcNow;
        }

        dbContext.StockOperations.Add(new StockOperation
        {
            IdempotencyKey = request.IdempotencyKey,
            Type = operationType
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Stock operation {OperationType} completed with key {IdempotencyKey}",
            operationType, request.IdempotencyKey);

        return new StockCommandResponse(false, $"Stock {operationType} completed successfully.");
    }
}
