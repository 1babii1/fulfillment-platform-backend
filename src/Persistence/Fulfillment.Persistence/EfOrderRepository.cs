using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Persistence;

public sealed class EfOrderRepository(FulfillmentDbContext db) : IOrderRepository
{
    public void Add(Order order)
    {
        db.Orders.Add(new OrderRecord
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            Lines = order.Lines.Select(line => new OrderLineRecord
            {
                VariantId = line.VariantId,
                Quantity = line.Quantity
            }).ToList()
        });
        db.SaveChanges();
    }

    public Order? Find(Guid orderId)
    {
        OrderRecord? record = db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .SingleOrDefault(order => order.Id == orderId);

        return record is null ? null : ToDomain(record);
    }

    public void Update(Order order)
    {
        OrderRecord? record = db.Orders.Find(order.Id);
        if (record is null)
        {
            throw new InvalidOperationException($"Order '{order.Id}' does not exist.");
        }

        record.Status = order.Status;
        db.SaveChanges();
    }

    private static Order ToDomain(OrderRecord record) =>
        Order.Rehydrate(
            record.Id,
            record.CustomerId,
            record.Status,
            record.CreatedAt,
            record.Lines.Select(line => new OrderLine(line.VariantId, line.Quantity)).ToArray());
}
