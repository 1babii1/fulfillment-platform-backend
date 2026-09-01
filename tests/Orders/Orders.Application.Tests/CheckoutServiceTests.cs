using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace Orders.Application.Tests;

public sealed class CheckoutServiceTests
{
    [Fact]
    public void Checkout_WhenStockIsAvailable_ReservesStockAndPersistsOrder()
    {
        Guid variantId = Guid.CreateVersion7();
        InventoryStock stock = StockWithOnHand(variantId, 5);
        InMemoryOrderRepository orders = new();
        CheckoutService service = CreateService(stock, orders);

        Result<Order> result = service.Checkout(new CheckoutCommand(Guid.CreateVersion7(), [new OrderLine(variantId, 2)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, stock.Reserved);
        Assert.Equal(3, stock.Available);
        Assert.Same(result.Value, orders.Find(result.Value.Id));
    }

    [Fact]
    public void Checkout_WhenLaterReservationFails_ReleasesEarlierReservationsAndDoesNotPersistOrder()
    {
        Guid availableVariantId = Guid.CreateVersion7();
        Guid unavailableVariantId = Guid.CreateVersion7();
        InventoryStock availableStock = StockWithOnHand(availableVariantId, 5);
        InventoryStock unavailableStock = StockWithOnHand(unavailableVariantId, 1);
        InMemoryOrderRepository orders = new();
        CheckoutService service = CreateService(availableStock, unavailableStock, orders);

        Result<Order> result = service.Checkout(new CheckoutCommand(
            Guid.CreateVersion7(),
            [new OrderLine(availableVariantId, 2), new OrderLine(unavailableVariantId, 2)]));

        Assert.True(result.IsFailure);
        Assert.Equal(0, availableStock.Reserved);
        Assert.Equal(0, orders.Count);
    }

    [Fact]
    public void Checkout_WhenOrderIsInvalid_DoesNotMutateInventory()
    {
        Guid variantId = Guid.CreateVersion7();
        InventoryStock stock = StockWithOnHand(variantId, 5);
        CheckoutService service = CreateService(stock, new InMemoryOrderRepository());

        Result<Order> result = service.Checkout(new CheckoutCommand(Guid.CreateVersion7(), []));

        Assert.True(result.IsFailure);
        Assert.Equal(0, stock.Reserved);
        Assert.Equal(5, stock.Available);
    }

    private static CheckoutService CreateService(InventoryStock stock, InMemoryOrderRepository orders) =>
        new(new InMemoryInventoryReservationStore([stock]), orders);

    private static CheckoutService CreateService(
        InventoryStock firstStock,
        InventoryStock secondStock,
        InMemoryOrderRepository orders) =>
        new(new InMemoryInventoryReservationStore([firstStock, secondStock]), orders);

    private static InventoryStock StockWithOnHand(Guid variantId, int quantity)
    {
        InventoryStock stock = new(variantId);
        Result result = stock.Receive(quantity);
        Assert.True(result.IsSuccess);
        return stock;
    }
}
