using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Orders.Application;

public interface IOrderRepository
{
    void Add(Order order);

    Order? Find(Guid orderId);
}
