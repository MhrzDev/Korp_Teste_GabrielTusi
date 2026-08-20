namespace BillingService.Models;

public sealed class PrintOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long InvoiceId { get; set; }
    public required string IdempotencyKey { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

