using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    BillingDbContext context,
    PasswordService passwordService,
    JwtTokenService jwtTokenService
) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var employee = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentEmployee =>
                    currentEmployee.Email == email &&
                    currentEmployee.IsActive,
                cancellationToken
            );

        if (
            employee is null ||
            !passwordService.Verify(
                request.Password,
                employee.PasswordHash
            )
        )
        {
            return Unauthorized(new
            {
                message = "E-mail ou senha inválidos."
            });
        }

        var tokenResult = jwtTokenService.Create(employee);

        return Ok(new
        {
            accessToken = tokenResult.Token,
            expiresAt = tokenResult.ExpiresAt,
            employee = new
            {
                employee.Id,
                employee.Name,
                employee.Email,
                employee.Role
            }
        });
    }
}