namespace FulfillmentPlatform.Orders.Domain;

public sealed record OrderLine(Guid VariantId, int Quantity);
