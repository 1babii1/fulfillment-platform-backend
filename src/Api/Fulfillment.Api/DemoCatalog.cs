using FulfillmentPlatform.Catalog.Domain;

namespace FulfillmentPlatform.Api;

public sealed class DemoCatalog
{
    private readonly List<DemoCatalogItem> _items;

    public DemoCatalog()
    {
        _items =
        [
            CreateItem("linen-shirt-sand-s", "Linen shirt — Sand / S", 12),
            CreateItem("linen-shirt-sand-m", "Linen shirt — Sand / M", 8),
            CreateItem("linen-trousers-sand-m", "Linen trousers — Sand / M", 5)
        ];
    }

    public IReadOnlyCollection<DemoCatalogItem> Items => _items.AsReadOnly();

    public IEnumerable<InventoryStock> Stocks => _items.Select(item => item.Stock);

    private static DemoCatalogItem CreateItem(string sku, string name, int onHand)
    {
        Guid variantId = Guid.CreateVersion7();
        InventoryStock stock = new(variantId);
        _ = stock.Receive(onHand);
        return new DemoCatalogItem(variantId, sku, name, stock);
    }
}

public sealed record DemoCatalogItem(Guid VariantId, string Sku, string Name, InventoryStock Stock);
