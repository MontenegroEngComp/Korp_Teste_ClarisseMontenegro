using System.ComponentModel.DataAnnotations.Schema;

namespace Korp.Billing.Api.Models;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public long Number { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public Guid? IssuedByEmployeeId { get; set; }

    public string? IssuedByName { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } =
        new List<InvoiceItem>();

    [NotMapped]
    public decimal Total =>
        Items.Sum(item => item.Subtotal);
}