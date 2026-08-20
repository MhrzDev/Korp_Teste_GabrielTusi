using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockController(StockService stockService) : ControllerBase
{
    [HttpPost("reserve")]
    public async Task<ActionResult<StockCommandResponse>> Reserve(
        StockCommandRequest request,
        [FromQuery] bool simulateFailure,
        CancellationToken cancellationToken)
    {
        if (simulateFailure)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Simulated inventory service failure. No stock was changed." });

        return Ok(await stockService.ReserveAsync(request, cancellationToken));
    }

    [HttpPost("release")]
    public async Task<ActionResult<StockCommandResponse>> Release(
        StockCommandRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await stockService.ReleaseAsync(request, cancellationToken));
    }
}

