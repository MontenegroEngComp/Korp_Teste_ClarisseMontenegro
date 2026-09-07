using System.ComponentModel.DataAnnotations;

namespace Korp.Stock.Api.Contracts;

public sealed class DecreaseStockBatchItemRequest
{
    [Required(ErrorMessage = "O produto é obrigatório.")]
    public Guid? ProductId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "A quantidade deve ser maior que zero."
    )]
    public int Quantity { get; set; }
}