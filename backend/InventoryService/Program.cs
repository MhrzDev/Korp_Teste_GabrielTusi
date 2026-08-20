using System.Text.Json;
using InventoryService.Data;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<StockService>();
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (KeyNotFoundException exception)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Resource not found",
            status = 404,
            detail = exception.Message
        });
    }
    catch (InvalidOperationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Stock operation rejected",
            status = 409,
            detail = exception.Message
        });
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Unhandled inventory service error");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title = "Unexpected error",
            status = 500,
            detail = "The inventory service could not complete the request."
        }));
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

await ApplyMigrationsWithRetryAsync(app.Services, app.Logger);
await app.RunAsync();

static async Task ApplyMigrationsWithRetryAsync(IServiceProvider services, ILogger logger)
{
    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
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
