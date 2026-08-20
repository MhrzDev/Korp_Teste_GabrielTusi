using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockOperation> StockOperations => Set<StockOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "\"Stock\" >= 0"));
        });

        modelBuilder.Entity<StockOperation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdempotencyKey, x.Type }).IsUnique();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120);
            entity.Property(x => x.Type).HasMaxLength(20);
        });
    }
}

