using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public sealed record StockLineRequest(
    Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity);

public sealed record StockCommandRequest(
    [Required, MaxLength(120)] string IdempotencyKey,
    [MinLength(1)] IReadOnlyCollection<StockLineRequest> Items);

public sealed record StockCommandResponse(bool AlreadyProcessed, string Message);

