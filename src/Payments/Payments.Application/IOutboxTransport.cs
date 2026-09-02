namespace FulfillmentPlatform.Payments.Application;

public interface IOutboxTransport
{
    Task PublishAsync(OutboxEnvelope message, CancellationToken cancellationToken);
}

public sealed record OutboxEnvelope(
    Guid Id,
    Guid OrderId,
    string Type,
    string Payload,
    DateTimeOffset OccurredAt);
