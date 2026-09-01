using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Orders.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines;

    private Order(Guid id, Guid customerId, List<OrderLine> lines)
    {
        Id = id;
        CustomerId = customerId;
        _lines = lines;
        Status = OrderStatus.PendingPayment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Result<Order> Create(Guid customerId, IReadOnlyCollection<OrderLine> lines)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.EmptyOrder());
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            return Result.Failure<Order>(OrderErrors.InvalidQuantity());
        }

        if (lines.Select(line => line.VariantId).Distinct().Count() != lines.Count)
        {
            return Result.Failure<Order>(OrderErrors.DuplicateVariant());
        }

        return Result.Success(new Order(Guid.CreateVersion7(), customerId, lines.ToList()));
    }

    public Result ConfirmPayment()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Confirmed));
        }

        Status = OrderStatus.Confirmed;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            return Result.Failure(OrderErrors.InvalidTransition(Status, OrderStatus.Cancelled));
        }

        Status = OrderStatus.Cancelled;
        return Result.Success();
    }
}
