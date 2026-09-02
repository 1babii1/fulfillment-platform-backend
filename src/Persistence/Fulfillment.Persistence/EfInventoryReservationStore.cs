using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

/// <summary>
///     PostgreSQL-backed reservation store. Conditional updates keep availability checks and
///     counter changes in one statement, making reservations safe across API instances.
/// </summary>
public sealed class EfInventoryReservationStore(FulfillmentDbContext db) : IInventoryReservationStore
{
    public Result Reserve(Guid variantId, int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        int changedRows = db.Database.ExecuteSqlInterpolated($"""
            UPDATE inventory_items
            SET reserved = reserved + {quantity}
            WHERE variant_id = {variantId}
              AND on_hand - reserved >= {quantity}
            """);

        if (changedRows == 1)
        {
            return Result.Success();
        }

        return db.InventoryItems.Any(item => item.VariantId == variantId)
            ? Result.Failure(CatalogErrors.InsufficientStock())
            : Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
    }

    public Result Release(Guid variantId, int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        int changedRows = db.Database.ExecuteSqlInterpolated($"""
            UPDATE inventory_items
            SET reserved = reserved - {quantity}
            WHERE variant_id = {variantId}
              AND reserved >= {quantity}
            """);

        if (changedRows == 1)
        {
            return Result.Success();
        }

        return db.InventoryItems.Any(item => item.VariantId == variantId)
            ? Result.Failure(CatalogErrors.ReservationExceedsReserved())
            : Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
    }
}
