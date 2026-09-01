using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Orders.Application;

/// <summary>
///     Demo-only inventory adapter. It serializes reservations in-process; persistent adapters
///     must enforce the same invariant with a conditional database update.
/// </summary>
public sealed class InMemoryInventoryReservationStore : IInventoryReservationStore
{
    private readonly Dictionary<Guid, InventoryStock> _stocks;
    private readonly Lock _gate = new();

    public InMemoryInventoryReservationStore(IEnumerable<InventoryStock> stocks)
    {
        _stocks = stocks.ToDictionary(stock => stock.VariantId);
    }

    public Result Reserve(Guid variantId, int quantity)
    {
        lock (_gate)
        {
            return _stocks.TryGetValue(variantId, out InventoryStock? stock)
                ? stock.Reserve(quantity)
                : Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
        }
    }

    public Result Release(Guid variantId, int quantity)
    {
        lock (_gate)
        {
            return _stocks.TryGetValue(variantId, out InventoryStock? stock)
                ? stock.Release(quantity)
                : Result.Failure(Error.NotFound("catalog.inventory.not_found", "Inventory stock was not found."));
        }
    }
}
