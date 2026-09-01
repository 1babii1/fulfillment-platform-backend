using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

public sealed class FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options) : DbContext(options)
{
    public DbSet<InventoryRecord> InventoryItems => Set<InventoryRecord>();

    public DbSet<OrderRecord> Orders => Set<OrderRecord>();

    public DbSet<OrderLineRecord> OrderLines => Set<OrderLineRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.ToTable("inventory_items", table =>
                table.HasCheckConstraint("CK_inventory_items_non_negative", "on_hand >= reserved AND reserved >= 0"));
            entity.HasKey(item => item.VariantId);
            entity.Property(item => item.VariantId).HasColumnName("variant_id");
            entity.Property(item => item.Sku).HasColumnName("sku");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.OnHand).HasColumnName("on_hand");
            entity.Property(item => item.Reserved).HasColumnName("reserved");
            entity.Property(item => item.Sku).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(256).IsRequired();
            entity.HasIndex(item => item.Sku).IsUnique();
        });

        modelBuilder.Entity<OrderRecord>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).HasColumnName("id");
            entity.Property(order => order.CustomerId).HasColumnName("customer_id");
            entity.Property(order => order.Status).HasColumnName("status");
            entity.Property(order => order.CreatedAt).HasColumnName("created_at");
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(order => order.CreatedAt).IsRequired();
            entity.HasIndex(order => order.CustomerId);
            entity.HasMany(order => order.Lines)
                .WithOne()
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineRecord>(entity =>
        {
            entity.ToTable("order_lines");
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Id).HasColumnName("id");
            entity.Property(line => line.OrderId).HasColumnName("order_id");
            entity.Property(line => line.VariantId).HasColumnName("variant_id");
            entity.Property(line => line.Quantity).HasColumnName("quantity");
            entity.Property(line => line.Quantity).IsRequired();
            entity.HasIndex(line => new { line.OrderId, line.VariantId }).IsUnique();
        });
    }
}
