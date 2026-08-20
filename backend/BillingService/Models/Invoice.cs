namespace BillingService.Models;

public enum InvoiceStatus
{
    Open = 0,
    Closed = 1
}

public sealed class Invoice
{
    public long Id { get; set; }
    public string? Number { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public List<InvoiceItem> Items { get; set; } = [];
}

public sealed class InvoiceItem
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductDescription { get; set; }
    public int Quantity { get; set; }
}
