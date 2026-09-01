using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace Orders.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_WithValidLines_CreatesPendingPaymentOrder()
    {
        Result<Order> result = Order.Create(Guid.CreateVersion7(), [new OrderLine(Guid.CreateVersion7(), 2)]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.PendingPayment, result.Value.Status);
        Assert.Single(result.Value.Lines);
    }

    [Fact]
    public void Create_WithoutLines_ReturnsValidationFailure()
    {
        Result<Order> result = Order.Create(Guid.CreateVersion7(), []);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.lines.empty", result.Error?.Code);
    }

    [Fact]
    public void Create_WithNonPositiveQuantity_ReturnsValidationFailure()
    {
        Result<Order> result = Order.Create(Guid.CreateVersion7(), [new OrderLine(Guid.CreateVersion7(), 0)]);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.lines.invalid_quantity", result.Error?.Code);
    }

    [Fact]
    public void Create_WithDuplicateVariant_ReturnsValidationFailure()
    {
        Guid variantId = Guid.CreateVersion7();
        Result<Order> result = Order.Create(Guid.CreateVersion7(), [new OrderLine(variantId, 1), new OrderLine(variantId, 2)]);

        Assert.True(result.IsFailure);
        Assert.Equal("orders.lines.duplicate_variant", result.Error?.Code);
    }

    [Fact]
    public void ConfirmPayment_FromPendingPayment_ConfirmsOrder()
    {
        Order order = CreateOrder();

        Result result = order.ConfirmPayment();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Cancel_FromPendingPayment_CancelsOrder()
    {
        Order order = CreateOrder();

        Result result = order.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void ConfirmPayment_AfterCancellation_ReturnsConflict()
    {
        Order order = CreateOrder();
        _ = order.Cancel();

        Result result = order.ConfirmPayment();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    private static Order CreateOrder() =>
        Order.Create(Guid.CreateVersion7(), [new OrderLine(Guid.CreateVersion7(), 1)]).Value;
}
