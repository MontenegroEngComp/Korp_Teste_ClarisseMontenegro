using System.ComponentModel.DataAnnotations;
using Korp.Billing.Api.Models;

namespace Korp.Billing.Api.Contracts;

public sealed class CreateEmployeeRequest
{
    [Required(ErrorMessage = "O nome do funcionário é obrigatório.")]
    [StringLength(
        150,
        MinimumLength = 3,
        ErrorMessage = "O nome deve possuir entre 3 e 150 caracteres."
    )]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [RegularExpression(
        @"^\d{11}$",
        ErrorMessage = "O CPF deve possuir exatamente 11 números."
    )]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(
        200,
        ErrorMessage = "O e-mail deve possuir no máximo 200 caracteres."
    )]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [StringLength(
        20,
        MinimumLength = 8,
        ErrorMessage = "O telefone deve possuir entre 8 e 20 caracteres."
    )]
    public string Phone { get; set; } = string.Empty;

        [EnumDataType(
        typeof(EmployeeRole),
            ErrorMessage = "Informe um perfil válido."
        )]
        public EmployeeRole Role { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "A senha deve possuir entre 8 e 100 caracteres."
        )]
        public string Password { get; set; } = string.Empty;
}
