using System.ComponentModel.DataAnnotations;

namespace Korp.Billing.Api.Contracts;

public sealed class CreateInvoiceRequest
{
    [Required(ErrorMessage = "Os itens da nota são obrigatórios.")]
    [MinLength(
        1,
        ErrorMessage = "A nota deve possuir pelo menos um item."
    )]
    public List<CreateInvoiceItemRequest> Items { get; set; } = [];
}