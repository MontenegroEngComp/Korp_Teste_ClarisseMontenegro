using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Korp.Billing.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Korp.Billing.Api.Services;

public sealed class JwtTokenService(
    IConfiguration configuration
)
{
    public (string Token, DateTime ExpiresAt) Create(
        Employee employee
    )
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "A chave JWT não foi configurada."
            );

        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "O emissor JWT não foi configurado."
            );

        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "A audiência JWT não foi configurada."
            );

        var expiresAt = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                employee.Id.ToString()
            ),
            new(
                ClaimTypes.NameIdentifier,
                employee.Id.ToString()
            ),
            new(
                ClaimTypes.Name,
                employee.Name
            ),
            new(
                ClaimTypes.Email,
                employee.Email
            ),
            new(
                ClaimTypes.Role,
                employee.Role.ToString()
            )
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var token = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        return (token, expiresAt);
    }
}