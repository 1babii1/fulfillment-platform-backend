using System.Text.Json;
using FulfillmentPlatform.Payments.Application;

namespace FulfillmentPlatform.Persistence;

public sealed class EfOutboxEventPublisher(FulfillmentDbContext db) : IOrderEventPublisher
{
    public void Publish(OrderConfirmedEvent @event)
    {
        db.OutboxMessages.Add(new OutboxMessageRecord
        {
            Id = Guid.CreateVersion7(),
            OrderId = @event.OrderId,
            Type = nameof(OrderConfirmedEvent),
            Payload = JsonSerializer.Serialize(@event),
            OccurredAt = @event.OccurredAt,
            AttemptCount = 0
        });

        // The tracked order status and this message are committed by one EF Core SaveChanges transaction.
        db.SaveChanges();
    }
}
