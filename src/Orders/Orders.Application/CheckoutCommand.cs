using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Orders.Application;

public sealed record CheckoutCommand(Guid CustomerId, IReadOnlyCollection<OrderLine> Lines);
