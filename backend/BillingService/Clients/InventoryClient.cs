using System.Net;
using System.Net.Http.Json;

namespace BillingService.Clients;

public sealed record StockLine(Guid ProductId, int Quantity);
public sealed record StockCommand(string IdempotencyKey, IReadOnlyCollection<StockLine> Items);
public sealed record InventoryProduct(Guid Id, string Code, string Description, int Stock);

public sealed class InventoryUnavailableException(string message) : Exception(message);
public sealed class StockRejectedException(string message) : Exception(message);

public sealed class InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger)
{
    public async Task<InventoryProduct> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);
                if (response.IsSuccessStatusCode)
                    return (await response.Content.ReadFromJsonAsync<InventoryProduct>(cancellationToken: cancellationToken))!;

                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new StockRejectedException($"Product {productId} was not found in Inventory.");

                if (attempt == 3)
                    throw new InventoryUnavailableException("Inventory service could not validate the product.");
            }
            catch (StockRejectedException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == 3) throw new InventoryUnavailableException("Inventory service timed out.");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Product validation failed on attempt {Attempt}/3", attempt);
                if (attempt == 3)
                    throw new InventoryUnavailableException("Inventory service is temporarily unavailable.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
        }

        throw new InventoryUnavailableException("Inventory service could not validate the product.");
    }

    public async Task ReserveAsync(
        string idempotencyKey,
        IReadOnlyCollection<StockLine> items,
        bool simulateFailure,
        CancellationToken cancellationToken)
    {
        var endpoint = $"api/stock/reserve?simulateFailure={simulateFailure.ToString().ToLowerInvariant()}";
        var command = new StockCommand(idempotencyKey, items);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await httpClient.PostAsJsonAsync(endpoint, command, cancellationToken);
                if (response.IsSuccessStatusCode) return;

                var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: cancellationToken);
                var message = problem?.Detail ?? problem?.Message ?? "Inventory service rejected the operation.";

                if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
                    throw new StockRejectedException(message);

                if (attempt == 3)
                    throw new InventoryUnavailableException(message);
            }
            catch (StockRejectedException)
            {
                throw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == 3) throw new InventoryUnavailableException("Inventory service timed out.");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Inventory request failed on attempt {Attempt}/3", attempt);
                if (attempt == 3)
                    throw new InventoryUnavailableException("Inventory service is temporarily unavailable.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
        }
    }

    private sealed record ApiProblem(string? Detail, string? Message);
}
