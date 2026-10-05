using System.ComponentModel.DataAnnotations;

namespace Korp.Stock.Api.Contracts;

public sealed class IncreaseStockBatchRequest
{
    [Required(ErrorMessage = "Os itens são obrigatórios.")]
    [MinLength(
        1,
        ErrorMessage = "Informe pelo menos um item."
    )]
    public List<IncreaseStockBatchItemRequest> Items { get; set; } = [];
}
