namespace FulfillmentPlatform.Catalog.Application;

public sealed record CatalogItem(Guid VariantId, string Sku, string Name, int Available);
