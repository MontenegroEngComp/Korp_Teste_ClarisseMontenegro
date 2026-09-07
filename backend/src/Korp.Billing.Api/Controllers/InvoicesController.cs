using Korp.Billing.Api.Clients;
using Korp.Billing.Api.Contracts;
using Korp.Billing.Api.Data;
using Korp.Billing.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Controllers;

[ApiController]
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

        var invoice = new Invoice();

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
}