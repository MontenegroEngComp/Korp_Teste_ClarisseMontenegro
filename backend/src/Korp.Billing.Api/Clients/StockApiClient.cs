using System.Net;
using System.Net.Http.Json;

namespace Korp.Billing.Api.Clients;

public sealed class StockApiClient(HttpClient httpClient)
{
    public async Task<StockProductResponse?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken
    )
    {
        using var response = await httpClient.GetAsync(
            $"api/products/{productId}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<StockProductResponse>(
                cancellationToken
            );
    }
        public async Task<DecreaseStockResult> DecreaseStockBatchAsync(
        IReadOnlyCollection<DecreaseStockItem> items,
        CancellationToken cancellationToken
        )
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/stock/decrease-batch",
            new
            {
                items
            },
            cancellationToken
        );

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent =>
                DecreaseStockResult.Success,

            HttpStatusCode.NotFound =>
                DecreaseStockResult.ProductNotFound,

            HttpStatusCode.Conflict =>
                DecreaseStockResult.InsufficientStock,

            _ => throw new HttpRequestException(
                $"Falha ao diminuir o estoque. Status: {(int)response.StatusCode}."
            )
        };
    }
}