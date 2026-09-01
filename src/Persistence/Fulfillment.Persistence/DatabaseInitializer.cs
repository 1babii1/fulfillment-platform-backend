using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

public static class DatabaseInitializer
{
    public static void Initialize(FulfillmentDbContext db)
    {
        db.Database.Migrate();

        if (db.InventoryItems.Any())
        {
            return;
        }

        db.InventoryItems.AddRange(
            new InventoryRecord { VariantId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Sku = "linen-shirt-sand-s", Name = "Linen shirt — Sand / S", OnHand = 12 },
            new InventoryRecord { VariantId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Sku = "linen-shirt-sand-m", Name = "Linen shirt — Sand / M", OnHand = 8 },
            new InventoryRecord { VariantId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Sku = "linen-trousers-sand-m", Name = "Linen trousers — Sand / M", OnHand = 5 });
        db.SaveChanges();
    }
}
