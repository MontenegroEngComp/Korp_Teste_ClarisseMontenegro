namespace Korp.Stock.Api.Models;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}