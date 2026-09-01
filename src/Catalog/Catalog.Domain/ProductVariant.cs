namespace FulfillmentPlatform.Catalog.Domain;

public sealed record ProductVariant(Guid Id, Guid ProductId, string Sku, string Name);
