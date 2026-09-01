using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Catalog.Domain;

/// <summary>
///     In-memory domain model for stock counters. In a persistent implementation, reservation
///     must use a conditional database update to preserve this invariant under concurrency.
/// </summary>
public sealed class InventoryStock
{
    public InventoryStock(Guid variantId)
    {
        VariantId = variantId;
    }

    public Guid VariantId { get; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available => OnHand - Reserved;

    public Result Receive(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        OnHand += quantity;
        return Result.Success();
    }

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        if (quantity > Available)
        {
            return Result.Failure(CatalogErrors.InsufficientStock());
        }

        Reserved += quantity;
        return Result.Success();
    }

    public Result Release(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        if (quantity > Reserved)
        {
            return Result.Failure(CatalogErrors.ReservationExceedsReserved());
        }

        Reserved -= quantity;
        return Result.Success();
    }

    public Result CommitReservation(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.InvalidQuantity());
        }

        if (quantity > Reserved)
        {
            return Result.Failure(CatalogErrors.ReservationExceedsReserved());
        }

        Reserved -= quantity;
        OnHand -= quantity;
        return Result.Success();
    }
}
