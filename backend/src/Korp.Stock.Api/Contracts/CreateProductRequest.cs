using System.ComponentModel.DataAnnotations;

namespace Korp.Stock.Api.Contracts;

public sealed class CreateProductRequest
{
    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(
        50,
        MinimumLength = 2,
        ErrorMessage = "O código deve ter entre 2 e 50 caracteres."
    )]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(
        200,
        MinimumLength = 3,
        ErrorMessage = "A descrição deve ter entre 3 e 200 caracteres."
    )]
    public string Description { get; set; } = string.Empty;

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "O saldo não pode ser negativo."
    )]
    public int StockQuantity { get; set; }

    [Range(
    0.01,
    99999999.99,
    ErrorMessage = "O preço deve ser maior que zero."
    )]
    public decimal UnitPrice { get; set; }
}
