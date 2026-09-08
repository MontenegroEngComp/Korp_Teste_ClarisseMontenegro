using System.ComponentModel.DataAnnotations;

namespace Korp.Billing.Api.Contracts;

public sealed class UpdateEmployeePasswordRequest
{
    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "A senha deve possuir entre 8 e 100 caracteres."
    )]
    public string Password { get; set; } = string.Empty;
}