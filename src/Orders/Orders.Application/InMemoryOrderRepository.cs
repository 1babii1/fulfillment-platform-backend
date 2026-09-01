using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Orders.Application;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public int Count => _orders.Count;

    public void Add(Order order) => _orders.Add(order.Id, order);

    public Order? Find(Guid orderId) => _orders.GetValueOrDefault(orderId);
}
