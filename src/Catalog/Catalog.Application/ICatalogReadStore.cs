namespace FulfillmentPlatform.Catalog.Application;

public interface ICatalogReadStore
{
    IReadOnlyCollection<CatalogItem> GetAvailableItems();
}
