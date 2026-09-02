using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Orders.Application;

public interface IOrderLockingExecutor
{
    T Execute<T>(Guid orderId, Func<Order?, T> action);
}
