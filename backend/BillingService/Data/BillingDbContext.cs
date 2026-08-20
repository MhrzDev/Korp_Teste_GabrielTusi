using BillingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Data;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<PrintOperation> PrintOperations => Set<PrintOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductCode).HasMaxLength(50);
            entity.Property(x => x.ProductDescription).HasMaxLength(200);
            entity.ToTable(t => t.HasCheckConstraint("CK_InvoiceItem_Quantity", "\"Quantity\" > 0"));
        });

        modelBuilder.Entity<PrintOperation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InvoiceId).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120);
        });
    }
}
