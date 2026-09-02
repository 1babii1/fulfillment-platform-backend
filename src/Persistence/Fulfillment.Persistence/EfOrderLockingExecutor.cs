using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

public sealed class EfOrderLockingExecutor(FulfillmentDbContext db) : IOrderLockingExecutor
{
    public T Execute<T>(Guid orderId, Func<Order?, T> action)
    {
        using var transaction = db.Database.CurrentTransaction is null
            ? db.Database.BeginTransaction()
            : null;
        OrderRecord? record = db.Orders
            .FromSqlInterpolated($"""
                SELECT *
                FROM orders
                WHERE id = {orderId}
                FOR UPDATE
                """)
            .SingleOrDefault();

        if (record is null)
        {
            transaction?.Commit();
            return action(null);
        }

        IReadOnlyCollection<OrderLine> lines = db.OrderLines
            .Where(line => line.OrderId == record.Id)
            .Select(line => new OrderLine(line.VariantId, line.Quantity))
            .ToArray();
        Order order = Order.Rehydrate(record.Id, record.CustomerId, record.Status, record.CreatedAt, lines);

        T result = action(order);
        record.Status = order.Status;
        db.SaveChanges();
        transaction?.Commit();
        return result;
    }
}
