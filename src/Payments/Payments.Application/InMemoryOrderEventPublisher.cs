namespace FulfillmentPlatform.Payments.Application;

public sealed class InMemoryOrderEventPublisher : IOrderEventPublisher
{
    private readonly List<OrderConfirmedEvent> _events = [];

    public IReadOnlyCollection<OrderConfirmedEvent> Events => _events.AsReadOnly();

    public void Publish(OrderConfirmedEvent @event) => _events.Add(@event);
}
