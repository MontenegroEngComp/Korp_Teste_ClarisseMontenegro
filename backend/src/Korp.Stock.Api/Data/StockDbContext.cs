using Korp.Stock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Korp.Stock.Api.Data;

public sealed class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(product => product.Id);

            entity.HasIndex(product => product.Code)
                .IsUnique();

            entity.Property(product => product.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(product => product.Description)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(product => product.StockQuantity)
                .IsRequired();

            entity.Property(product => product.UnitPrice)
                .HasPrecision(10, 2)
                .IsRequired();
        });
    }
}