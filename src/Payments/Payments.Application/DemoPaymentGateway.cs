using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Payments.Application;

public sealed class DemoPaymentGateway(bool shouldSucceed = true) : IPaymentGateway
{
    public int ConfirmationAttempts { get; private set; }

    public Result<PaymentReceipt> Confirm(Order order)
    {
        ConfirmationAttempts++;

        return shouldSucceed
            ? Result.Success(new PaymentReceipt($"demo-pay-{order.Id:N}", DateTimeOffset.UtcNow))
            : Result.Failure<PaymentReceipt>(
                Error.Failure("payments.demo.declined", "The demo payment was declined."));
    }
}
