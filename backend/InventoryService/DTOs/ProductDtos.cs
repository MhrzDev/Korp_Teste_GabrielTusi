using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public sealed record CreateProductRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string Description,
    [Range(0, int.MaxValue)] int Stock);

public sealed record UpdateProductRequest(
    [Required, MaxLength(200)] string Description,
    [Range(0, int.MaxValue)] int Stock);

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Description,
    int Stock,
    DateTime UpdatedAtUtc);

