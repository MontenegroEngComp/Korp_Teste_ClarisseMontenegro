using System.ComponentModel.DataAnnotations;

namespace Korp.Stock.Api.Contracts;

public sealed class DecreaseStockBatchRequest
{
    [Required(ErrorMessage = "Os itens são obrigatórios.")]
    [MinLength(
        1,
        ErrorMessage = "Informe pelo menos um item."
    )]
    public List<DecreaseStockBatchItemRequest> Items { get; set; } = [];
}