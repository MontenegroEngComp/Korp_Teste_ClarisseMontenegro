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
}