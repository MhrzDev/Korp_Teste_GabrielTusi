using System.Data;
using BillingService.Clients;
using BillingService.Data;
using BillingService.DTOs;
using BillingService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(
    BillingDbContext dbContext,
    InventoryClient inventoryClient,
    ILogger<InvoicesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InvoiceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Items)
            .OrderByDescending(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        return Ok(invoices.Select(ToResponse));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return invoice is null ? NotFound() : Ok(ToResponse(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceResponse>> Create(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { message = "An invoice must contain at least one product." });

        var requestedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToArray();

        var consolidatedItems = new List<InvoiceItem>();
        foreach (var requestedItem in requestedItems)
        {
            var product = await inventoryClient.GetProductAsync(requestedItem.ProductId, cancellationToken);
            consolidatedItems.Add(new InvoiceItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductDescription = product.Description,
                Quantity = requestedItem.Quantity
            });
        }

        var invoice = new Invoice { Items = consolidatedItems };
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        invoice.Number = $"NF-{invoice.Id:000000}";
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Invoice {InvoiceNumber} created", invoice.Number);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, ToResponse(invoice));
    }

    [HttpPost("{id:long}/print")]
    public async Task<ActionResult<PrintInvoiceResponse>> Print(
        long id,
        PrintInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? $"print-{id}-{Guid.NewGuid():N}"
            : idempotencyKey.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var invoice = await dbContext.Invoices
            .FromSqlInterpolated($"SELECT * FROM \"Invoices\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (invoice is null) return NotFound();

        await dbContext.Entry(invoice).Collection(item => item.Items).LoadAsync(cancellationToken);

        if (invoice.Status == InvoiceStatus.Closed)
        {
            await transaction.CommitAsync(cancellationToken);
            return Ok(new PrintInvoiceResponse(ToResponse(invoice), true,
                "Invoice was already closed. Stock was not deducted again."));
        }

        var stockLines = invoice.Items
            .Select(item => new StockLine(item.ProductId, item.Quantity))
            .ToArray();

        // The service-level key stays stable even if the client retries with a new request key.
        // This closes the crash window between the remote stock reservation and local commit.
        await inventoryClient.ReserveAsync(
            $"invoice-{invoice.Id}",
            stockLines,
            request.SimulateInventoryFailure,
            cancellationToken);

        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAtUtc = DateTime.UtcNow;
        dbContext.PrintOperations.Add(new PrintOperation
        {
            InvoiceId = invoice.Id,
            IdempotencyKey = idempotencyKey
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Invoice {InvoiceNumber} closed and printed", invoice.Number);
        return Ok(new PrintInvoiceResponse(ToResponse(invoice), false,
            "Invoice printed successfully and stock updated."));
    }

    private static InvoiceResponse ToResponse(Invoice invoice) => new(
        invoice.Id,
        invoice.Number ?? $"NF-{invoice.Id:000000}",
        invoice.Status.ToString(),
        invoice.CreatedAtUtc,
        invoice.ClosedAtUtc,
        invoice.Items.Select(item => new InvoiceItemResponse(
            item.ProductId,
            item.ProductCode,
            item.ProductDescription,
            item.Quantity)).ToArray());
}
