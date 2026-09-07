namespace Korp.Billing.Api.Clients;

public sealed record StockProductResponse(
    Guid Id,
    string Code,
    string Description,
    int StockQuantity,
    decimal UnitPrice
);