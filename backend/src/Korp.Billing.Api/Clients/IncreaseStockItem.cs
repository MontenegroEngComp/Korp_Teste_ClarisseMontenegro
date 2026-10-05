namespace Korp.Billing.Api.Clients;

public sealed record IncreaseStockItem(
    Guid ProductId,
    int Quantity
);

public enum IncreaseStockResult
{
    Success,
    ProductNotFound
}
