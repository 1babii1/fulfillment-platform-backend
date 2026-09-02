using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

public static class DatabaseInitializer
{
    public static void Initialize(FulfillmentDbContext db)
    {
        db.Database.Migrate();

        db.Database.ExecuteSql($"""
            INSERT INTO inventory_items (variant_id, sku, name, on_hand, reserved)
            VALUES
                ({Guid.Parse("11111111-1111-1111-1111-111111111111")}, {"linen-shirt-sand-s"}, {"Linen shirt — Sand / S"}, {12}, {0}),
                ({Guid.Parse("22222222-2222-2222-2222-222222222222")}, {"linen-shirt-sand-m"}, {"Linen shirt — Sand / M"}, {8}, {0}),
                ({Guid.Parse("33333333-3333-3333-3333-333333333333")}, {"linen-trousers-sand-m"}, {"Linen trousers — Sand / M"}, {5}, {0})
            ON CONFLICT (variant_id) DO NOTHING
            """);
    }
}
