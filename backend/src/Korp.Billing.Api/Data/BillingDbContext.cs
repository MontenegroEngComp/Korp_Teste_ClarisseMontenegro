using Korp.Billing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Korp.Billing.Api.Data;

public sealed class BillingDbContext(
    DbContextOptions<BillingDbContext> options
) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.ToTable("invoices");

            builder.HasKey(invoice => invoice.Id);

            builder.Property(invoice => invoice.Number)
                .UseIdentityByDefaultColumn();

            builder.HasIndex(invoice => invoice.Number)
                .IsUnique();

            builder.Property(invoice => invoice.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(invoice => invoice.CreatedAt)
                .IsRequired();

            builder.HasMany(invoice => invoice.Items)
                .WithOne()
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceItem>(builder =>
        {
            builder.ToTable("invoice_items");

            builder.HasKey(item => item.Id);

            builder.Property(item => item.ProductCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(item => item.ProductDescription)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(item => item.Quantity)
                .IsRequired();

            builder.Property(item => item.UnitPrice)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.HasIndex(item => item.InvoiceId);
            builder.HasIndex(item => item.ProductId);
        });
    }
}