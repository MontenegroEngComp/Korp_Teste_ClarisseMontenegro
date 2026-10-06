using System.ComponentModel.DataAnnotations;
using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Services;

// Cria o primeiro administrador a partir da seção BootstrapAdmin da
// configuração. É idempotente: se já existir qualquer administrador,
// nada é criado nem alterado.
public sealed class AdminBootstrapper(
    BillingDbContext context,
    PasswordService passwordService,
    IConfiguration configuration,
    ILogger<AdminBootstrapper> logger
)
{
    public async Task RunAsync(
        CancellationToken cancellationToken = default
    )
    {
        var administratorExists = await context.Employees
            .AnyAsync(
                employee => employee.Role == EmployeeRole.Administrator,
                cancellationToken
            );

        if (administratorExists)
        {
            logger.LogInformation(
                "Já existe um administrador cadastrado. " +
                "O administrador inicial não foi criado."
            );

            return;
        }

        var section = configuration.GetSection("BootstrapAdmin");

        // Reaproveita as mesmas regras de validação do cadastro de funcionários.
        var request = new CreateEmployeeRequest
        {
            Name = section["Name"]?.Trim() ?? string.Empty,
            Cpf = section["Cpf"]?.Trim() ?? string.Empty,
            Email = section["Email"]?.Trim().ToLowerInvariant() ?? string.Empty,
            Phone = section["Phone"]?.Trim() ?? string.Empty,
            Role = EmployeeRole.Administrator,
            Password = section["Password"] ?? string.Empty
        };

        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true
        );

        if (!isValid)
        {
            // As mensagens descrevem apenas a regra violada, nunca o valor.
            var errors = validationResults.Select(result =>
                $"BootstrapAdmin:{string.Join(", ", result.MemberNames)} - " +
                result.ErrorMessage
            );

            throw new InvalidOperationException(
                "Não foi possível criar o administrador inicial. " +
                "Revise as variáveis BOOTSTRAP_ADMIN_* no arquivo .env. " +
                string.Join(" ", errors)
            );
        }

        var employeeAlreadyExists = await context.Employees
            .AnyAsync(
                employee =>
                    employee.Cpf == request.Cpf ||
                    employee.Email == request.Email,
                cancellationToken
            );

        if (employeeAlreadyExists)
        {
            throw new InvalidOperationException(
                "Não foi possível criar o administrador inicial: " +
                "já existe um funcionário com o CPF ou e-mail configurado. " +
                "Nenhum funcionário existente foi alterado."
            );
        }

        var administrator = new Employee
        {
            Name = request.Name,
            Cpf = request.Cpf,
            Email = request.Email,
            Phone = request.Phone,
            Role = EmployeeRole.Administrator,
            PasswordHash = passwordService.Hash(request.Password)
        };

        context.Employees.Add(administrator);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Administrador inicial criado com o ID {EmployeeId}.",
            administrator.Id
        );
    }
}
