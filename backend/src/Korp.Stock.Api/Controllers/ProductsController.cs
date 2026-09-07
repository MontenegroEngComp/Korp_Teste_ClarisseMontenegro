using Korp.Stock.Api.Contracts;
using Korp.Stock.Api.Data;
using Korp.Stock.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Korp.Stock.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly StockDbContext _context;

    public ProductsController(StockDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(product => product.Description)
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken
            );

        if (product is null)
        {
            return NotFound(new
            {
                message = "Produto não encontrado."
            });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken
    )
    {
        var normalizedCode = request.Code
            .Trim()
            .ToUpperInvariant();

        var codeAlreadyExists = await _context.Products
            .AnyAsync(
                product => product.Code == normalizedCode,
                cancellationToken
            );

        if (codeAlreadyExists)
        {
            return Conflict(new
            {
                message = "Já existe um produto com esse código."
            });
        }

        var product = new Product
        {
            Code = normalizedCode,
            Description = request.Description.Trim(),
            StockQuantity = request.StockQuantity,
            UnitPrice = request.UnitPrice
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product
        );
    }
}