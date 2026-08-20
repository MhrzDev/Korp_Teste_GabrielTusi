namespace InventoryService.Models;

public sealed class StockOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string IdempotencyKey { get; set; }
    public required string Type { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

