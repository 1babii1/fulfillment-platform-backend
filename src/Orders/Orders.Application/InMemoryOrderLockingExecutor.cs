using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Orders.Application;

public sealed class InMemoryOrderLockingExecutor(IOrderRepository orders) : IOrderLockingExecutor
{
    public T Execute<T>(Guid orderId, Func<Order?, T> action) => action(orders.Find(orderId));
}
