using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Persistence;

/// <summary>
///     PostgreSQL-backed reservation store. The read-modify-write operation is intentionally
///     replaced with a conditional update in the next roadmap stage to make it safe across nodes.
/// </summary>
public sealed class EfInventoryReservationStore(FulfillmentDbContext db) : IInventoryReservationStore
{
    public Result Reserve(Guid variantId, int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        InventoryRecord? item = db.InventoryItems.Find(variantId);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
        }

        if (quantity > item.OnHand - item.Reserved)
        {
            return Result.Failure(CatalogErrors.InsufficientStock());
        }

        item.Reserved += quantity;
        db.SaveChanges();
        return Result.Success();
    }

    public Result Release(Guid variantId, int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        InventoryRecord? item = db.InventoryItems.Find(variantId);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
        }

        if (quantity > item.Reserved)
        {
            return Result.Failure(CatalogErrors.ReservationExceedsReserved());
        }

        item.Reserved -= quantity;
        db.SaveChanges();
        return Result.Success();
    }
}
