using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korp.Stock.Api.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockOperationsController(
    StockDbContext context
) : ControllerBase
{
    [HttpPost("decrease-batch")]
    public async Task<IActionResult> DecreaseBatch(
        DecreaseStockBatchRequest request,
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

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken
            );

        foreach (var requestedItem in request.Items)
        {
            var productId = requestedItem.ProductId!.Value;

            var affectedRows = await context.Products
                .Where(product =>
                    product.Id == productId &&
                    product.StockQuantity >= requestedItem.Quantity
                )
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            product => product.StockQuantity,
                            product =>
                                product.StockQuantity -
                                requestedItem.Quantity
                        )
                        .SetProperty(
                            product => product.UpdatedAt,
                            DateTime.UtcNow
                        ),
                    cancellationToken
                );

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(
                    cancellationToken
                );

                var productExists = await context.Products
                    .AsNoTracking()
                    .AnyAsync(
                        product => product.Id == productId,
                        cancellationToken
                    );

                if (!productExists)
                {
                    return NotFound(new
                    {
                        message =
                            $"O produto {productId} não foi encontrado."
                    });
                }

                return Conflict(new
                {
                    message =
                        $"O produto {productId} não possui estoque suficiente."
                });
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return NoContent();
    }
}
