using Korp.Billing.Api.Clients;
using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Korp.Billing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController(
    BillingDbContext context,
    StockApiClient stockApiClient
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Invoice>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        var invoices = await context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Items)
            .OrderByDescending(invoice => invoice.Number)
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Invoice>> GetById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var invoice = await context.Invoices
            .AsNoTracking()
            .Include(currentInvoice => currentInvoice.Items)
            .FirstOrDefaultAsync(
                currentInvoice => currentInvoice.Id == id,
                cancellationToken
            );

        if (invoice is null)
        {
            return NotFound(new
            {
                message = "Nota fiscal não encontrada."
            });
        }

        return Ok(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<Invoice>> Create(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken
    )
    {

        var employeeIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!Guid.TryParse(employeeIdValue, out var employeeId))
        {
            return Unauthorized(new
            {
                message = "Funcionário autenticado inválido."
            });
        }

        var employee = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                currentEmployee =>
                    currentEmployee.Id == employeeId &&
                    currentEmployee.IsActive,
                cancellationToken
            );

        if (employee is null)
        {
            return Unauthorized(new
            {
                message =
                    "Funcionário não encontrado ou desativado."
            });
        }

        var hasDuplicatedProducts = request.Items
                .GroupBy(item => item.ProductId)
                .Any(group => group.Count() > 1);

        if (hasDuplicatedProducts)
        {
            return BadRequest(new
            {
                message =
                    "O mesmo produto não pode aparecer mais de uma vez."
            });
        }

        var invoice = new Invoice
        {
            IssuedByEmployeeId = employee.Id,
            IssuedByName = employee.Name
        };

        foreach (var requestedItem in request.Items)
        {
            StockProductResponse? product;

            try
            {
                product = await stockApiClient.GetProductByIdAsync(
                    requestedItem.ProductId!.Value,
                    cancellationToken
                );
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message =
                            "O serviço de estoque está indisponível."
                    }
                );
            }

            if (product is null)
            {
                return BadRequest(new
                {
                    message =
                        $"O produto {requestedItem.ProductId} não existe."
                });
            }

            invoice.Items.Add(new InvoiceItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductDescription = product.Description,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.UnitPrice
            });
        }

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = invoice.Id },
            invoice
        );
    }
    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<Invoice>> Close(
    Guid id,
    CancellationToken cancellationToken
)
    {
        var invoice = await context.Invoices
            .Include(currentInvoice => currentInvoice.Items)
            .FirstOrDefaultAsync(
                currentInvoice => currentInvoice.Id == id,
                cancellationToken
            );

        if (invoice is null)
        {
            return NotFound(new
            {
                message = "Nota fiscal não encontrada."
            });
        }

        if (invoice.Status == InvoiceStatus.Closed)
        {
            return Conflict(new
            {
                message = "A nota fiscal já está fechada."
            });
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Conflict(new
            {
                message = "Uma nota fiscal cancelada não pode ser fechada."
            });
        }

        var stockItems = invoice.Items
            .Select(item => new DecreaseStockItem(
                item.ProductId,
                item.Quantity
            ))
            .ToList();

        DecreaseStockResult stockResult;

        try
        {
            stockResult = await stockApiClient
                .DecreaseStockBatchAsync(
                    stockItems,
                    cancellationToken
                );
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "Não foi possível acessar o serviço de estoque. " +
                        "A nota permanece aberta e pode ser processada novamente."
                }
            );
        }

        if (stockResult == DecreaseStockResult.ProductNotFound)
        {
            return Conflict(new
            {
                message =
                    "Um dos produtos da nota não existe mais no estoque."
            });
        }

        if (stockResult == DecreaseStockResult.InsufficientStock)
        {
            return Conflict(new
            {
                message =
                    "Um dos produtos não possui estoque suficiente. " +
                    "Nenhuma baixa foi realizada."
            });
        }

        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Ok(invoice);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<Invoice>> Cancel(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        // Os itens são carregados antes da transição para que nenhuma leitura
        // ocorra entre a marcação como cancelada e a devolução ao estoque.
        var invoice = await context.Invoices
            .AsNoTracking()
            .Include(currentInvoice => currentInvoice.Items)
            .FirstOrDefaultAsync(
                currentInvoice => currentInvoice.Id == id,
                cancellationToken
            );

        if (invoice is null)
        {
            return NotFound(new
            {
                message = "Nota fiscal não encontrada."
            });
        }

        // A transição Closed -> Cancelled é condicional no banco: apenas a
        // requisição que alterar a linha devolve os itens ao estoque.
        var affectedRows = await context.Invoices
            .Where(currentInvoice =>
                currentInvoice.Id == id &&
                currentInvoice.Status == InvoiceStatus.Closed
            )
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        currentInvoice => currentInvoice.Status,
                        InvoiceStatus.Cancelled
                    ),
                cancellationToken
            );

        if (affectedRows != 1)
        {
            var currentStatus = await context.Invoices
                .AsNoTracking()
                .Where(currentInvoice => currentInvoice.Id == id)
                .Select(currentInvoice => (InvoiceStatus?)currentInvoice.Status)
                .FirstOrDefaultAsync(cancellationToken);

            return currentStatus switch
            {
                null => NotFound(new
                {
                    message = "Nota fiscal não encontrada."
                }),

                InvoiceStatus.Open => Conflict(new
                {
                    message =
                        "A nota fiscal ainda está aberta. " +
                        "Somente notas fechadas podem ser canceladas."
                }),

                InvoiceStatus.Cancelled => Conflict(new
                {
                    message = "A nota fiscal já está cancelada."
                }),

                _ => Conflict(new
                {
                    message =
                        "A nota fiscal está sendo processada por outra operação. " +
                        "Tente novamente."
                })
            };
        }

        var stockItems = invoice.Items
            .Select(item => new IncreaseStockItem(
                item.ProductId,
                item.Quantity
            ))
            .ToList();

        IncreaseStockResult stockResult;

        try
        {
            stockResult = await stockApiClient
                .IncreaseStockBatchAsync(
                    stockItems,
                    cancellationToken
                );
        }
        catch (HttpRequestException)
        {
            if (!await TryRevertCancellationAsync(id))
            {
                return CancellationRevertFailed();
            }

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "Não foi possível devolver os produtos ao estoque. " +
                        "A nota permanece fechada e o cancelamento pode ser tentado novamente."
                }
            );
        }
        catch
        {
            await TryRevertCancellationAsync(id);
            throw;
        }

        if (stockResult == IncreaseStockResult.ProductNotFound)
        {
            if (!await TryRevertCancellationAsync(id))
            {
                return CancellationRevertFailed();
            }

            return Conflict(new
            {
                message =
                    "Um dos produtos da nota não existe mais no estoque. " +
                    "Nenhum produto foi devolvido e a nota permanece fechada."
            });
        }

        invoice.Status = InvoiceStatus.Cancelled;

        return Ok(invoice);
    }

    private async Task<bool> TryRevertCancellationAsync(Guid id)
    {
        try
        {
            // Usa CancellationToken.None para que a compensação não seja
            // interrompida caso o cliente encerre a requisição.
            var affectedRows = await context.Invoices
                .Where(currentInvoice =>
                    currentInvoice.Id == id &&
                    currentInvoice.Status == InvoiceStatus.Cancelled
                )
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            currentInvoice => currentInvoice.Status,
                            InvoiceStatus.Closed
                        ),
                    CancellationToken.None
                );

            return affectedRows == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private ObjectResult CancellationRevertFailed()
    {
        return StatusCode(
            StatusCodes.Status500InternalServerError,
            new
            {
                message =
                    "Não foi possível devolver os produtos ao estoque " +
                    "nem restaurar a nota como fechada. " +
                    "Verifique a nota e o estoque antes de tentar novamente."
            }
        );
    }
}