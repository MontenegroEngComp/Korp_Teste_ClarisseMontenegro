namespace Korp.Billing.Api.Clients;

public sealed record DecreaseStockItem(
    Guid ProductId,
    int Quantity
);

public enum DecreaseStockResult
{
    Success,
    ProductNotFound,
    InsufficientStock
}