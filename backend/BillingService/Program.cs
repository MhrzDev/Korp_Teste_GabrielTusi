using System.Net;
using System.Text.Json;
using BillingService.Clients;
using BillingService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InventoryUrl"]
        ?? throw new InvalidOperationException("Inventory service URL is missing."));
    client.Timeout = TimeSpan.FromSeconds(3);
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (StockRejectedException exception)
    {
        await WriteProblemAsync(context, 409, "Invoice could not be closed", exception.Message);
    }
    catch (InventoryUnavailableException exception)
    {
        await WriteProblemAsync(context, 503, "Inventory service unavailable",
            $"The invoice remains open. {exception.Message}");
    }
    catch (DbUpdateException exception)
    {
        app.Logger.LogWarning(exception, "Database update conflict");
        await WriteProblemAsync(context, 409, "Operation conflict",
            "The operation was already processed or conflicted with another request.");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Unhandled billing service error");
        await WriteProblemAsync(context, 500, "Unexpected error",
            "The billing service could not complete the request.");
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

await ApplyDatabaseWithRetryAsync(app.Services, app.Logger);
await app.RunAsync();

static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
{
    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(new { title, status, detail }));
}

static async Task ApplyDatabaseWithRetryAsync(IServiceProvider services, ILogger logger)
{
    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception exception) when (attempt < 10)
        {
            logger.LogWarning(exception, "Database unavailable. Attempt {Attempt}/10", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program { }
