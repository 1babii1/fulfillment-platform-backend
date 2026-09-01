using FulfillmentPlatform.Catalog.Application;

namespace FulfillmentPlatform.Persistence;

public sealed class EfCatalogReadStore(FulfillmentDbContext db) : ICatalogReadStore
{
    public IReadOnlyCollection<CatalogItem> GetAvailableItems() =>
        db.InventoryItems
            .Where(item => item.OnHand > item.Reserved)
            .OrderBy(item => item.Sku)
            .Select(item => new CatalogItem(item.VariantId, item.Sku, item.Name, item.OnHand - item.Reserved))
            .ToArray();
}
