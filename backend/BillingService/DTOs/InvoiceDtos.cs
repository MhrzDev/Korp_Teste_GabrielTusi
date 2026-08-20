using System.ComponentModel.DataAnnotations;

namespace BillingService.DTOs;

public sealed record CreateInvoiceItemRequest(
    Guid ProductId,
    [Required, MaxLength(50)] string ProductCode,
    [Required, MaxLength(200)] string ProductDescription,
    [Range(1, int.MaxValue)] int Quantity);

public sealed record CreateInvoiceRequest(
    [MinLength(1)] IReadOnlyCollection<CreateInvoiceItemRequest> Items);

public sealed record InvoiceItemResponse(
    Guid ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity);

public sealed record InvoiceResponse(
    long Id,
    string Number,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    IReadOnlyCollection<InvoiceItemResponse> Items);

public sealed record PrintInvoiceRequest(bool SimulateInventoryFailure = false);

public sealed record PrintInvoiceResponse(
    InvoiceResponse Invoice,
    bool AlreadyProcessed,
    string Message);

