namespace FulfillmentPlatform.Payments.Application;

public interface IOrderEventPublisher
{
    void Publish(OrderConfirmedEvent @event);
}
