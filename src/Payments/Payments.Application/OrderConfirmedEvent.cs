namespace FulfillmentPlatform.Payments.Application;

public sealed record OrderConfirmedEvent(Guid OrderId, string PaymentReference, DateTimeOffset OccurredAt);
