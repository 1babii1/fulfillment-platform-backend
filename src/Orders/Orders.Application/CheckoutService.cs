using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Orders.Application;

public sealed class CheckoutService(
    IInventoryReservationStore inventory,
    IOrderRepository orders)
{
    public Result<Order> Checkout(CheckoutCommand command)
    {
        Result<Order> orderResult = Order.Create(command.CustomerId, command.Lines);
        if (orderResult.IsFailure)
        {
            return orderResult;
        }

        List<OrderLine> reservedLines = [];
        foreach (OrderLine line in orderResult.Value.Lines)
        {
            Result reserveResult = inventory.Reserve(line.VariantId, line.Quantity);
            if (reserveResult.IsFailure)
            {
                ReleaseReservedLines(reservedLines);
                return Result.Failure<Order>(reserveResult.Error!);
            }

            reservedLines.Add(line);
        }

        orders.Add(orderResult.Value);
        return orderResult;
    }

    private void ReleaseReservedLines(IEnumerable<OrderLine> reservedLines)
    {
        foreach (OrderLine line in reservedLines)
        {
            _ = inventory.Release(line.VariantId, line.Quantity);
        }
    }
}
