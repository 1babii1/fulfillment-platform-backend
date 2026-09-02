using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Payments.Application;

public sealed class ConfirmOrderPaymentService(
    IOrderLockingExecutor orderLock,
    IPaymentGateway paymentGateway,
    IOrderEventPublisher events)
{
    public Result<PaymentReceipt> Confirm(Guid orderId)
    {
        return orderLock.Execute(orderId, order =>
        {
            if (order is null)
            {
                return Result.Failure<PaymentReceipt>(Error.NotFound("orders.order.not_found", "Order was not found."));
            }

            if (order.Status != OrderStatus.PendingPayment)
            {
                return Result.Failure<PaymentReceipt>(
                    OrderErrors.InvalidTransition(order.Status, OrderStatus.Confirmed));
            }

            Result<PaymentReceipt> paymentResult = paymentGateway.Confirm(order);
            if (paymentResult.IsFailure)
            {
                return paymentResult;
            }

            Result confirmationResult = order.ConfirmPayment();
            if (confirmationResult.IsFailure)
            {
                return Result.Failure<PaymentReceipt>(confirmationResult.Error!);
            }

            events.Publish(new OrderConfirmedEvent(order.Id, paymentResult.Value.Reference, paymentResult.Value.ConfirmedAt));
            return paymentResult;
        });
    }
}
