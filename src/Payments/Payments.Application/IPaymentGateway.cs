using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Payments.Application;

public interface IPaymentGateway
{
    Result<PaymentReceipt> Confirm(Order order);
}
