using System.ComponentModel.DataAnnotations.Schema;

namespace Korp.Billing.Api.Models;

public sealed class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Subtotal => Quantity * UnitPrice;
}