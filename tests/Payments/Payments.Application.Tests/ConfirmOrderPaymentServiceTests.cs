using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.Payments.Application;
using FulfillmentPlatform.SharedKernel;

namespace Payments.Application.Tests;

public sealed class ConfirmOrderPaymentServiceTests
{
    [Fact]
    public void Confirm_WhenPaymentSucceeds_ConfirmsOrderAndPublishesEvent()
    {
        (Order order, InMemoryOrderRepository orders) = CreatePendingOrder();
        InMemoryOrderEventPublisher events = new();
        ConfirmOrderPaymentService service = new(new InMemoryOrderLockingExecutor(orders), new DemoPaymentGateway(), events);

        Result<PaymentReceipt> result = service.Confirm(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        OrderConfirmedEvent @event = Assert.Single(events.Events);
        Assert.Equal(order.Id, @event.OrderId);
        Assert.Equal(result.Value.Reference, @event.PaymentReference);
    }

    [Fact]
    public void Confirm_WhenGatewayDeclines_LeavesOrderPendingAndDoesNotPublishEvent()
    {
        (Order order, InMemoryOrderRepository orders) = CreatePendingOrder();
        InMemoryOrderEventPublisher events = new();
        ConfirmOrderPaymentService service = new(new InMemoryOrderLockingExecutor(orders), new DemoPaymentGateway(shouldSucceed: false), events);

        Result<PaymentReceipt> result = service.Confirm(order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Empty(events.Events);
    }

    [Fact]
    public void Confirm_WhenCalledTwice_DoesNotPublishDuplicateEvent()
    {
        (Order order, InMemoryOrderRepository orders) = CreatePendingOrder();
        InMemoryOrderEventPublisher events = new();
        DemoPaymentGateway gateway = new();
        ConfirmOrderPaymentService service = new(new InMemoryOrderLockingExecutor(orders), gateway, events);

        Result<PaymentReceipt> first = service.Confirm(order.Id);
        Result<PaymentReceipt> second = service.Confirm(order.Id);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Single(events.Events);
        Assert.Equal(1, gateway.ConfirmationAttempts);
    }

    [Fact]
    public void Confirm_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        ConfirmOrderPaymentService service = new(
            new InMemoryOrderLockingExecutor(new InMemoryOrderRepository()),
            new DemoPaymentGateway(),
            new InMemoryOrderEventPublisher());

        Result<PaymentReceipt> result = service.Confirm(Guid.CreateVersion7());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    private static (Order Order, InMemoryOrderRepository Orders) CreatePendingOrder()
    {
        Guid variantId = Guid.CreateVersion7();
        InventoryStock stock = new(variantId);
        _ = stock.Receive(2);
        InMemoryOrderRepository orders = new();
        CheckoutService checkout = new(new InMemoryInventoryReservationStore([stock]), orders);
        Order order = checkout.Checkout(new CheckoutCommand(Guid.CreateVersion7(), [new OrderLine(variantId, 1)])).Value;

        return (order, orders);
    }
}
