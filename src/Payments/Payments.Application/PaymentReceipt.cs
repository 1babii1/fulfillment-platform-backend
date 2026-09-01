namespace FulfillmentPlatform.Payments.Application;

public sealed record PaymentReceipt(string Reference, DateTimeOffset ConfirmedAt);
