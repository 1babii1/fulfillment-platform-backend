using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.SharedKernel;

namespace Catalog.Domain.Tests;

public sealed class InventoryStockTests
{
    [Fact]
    public void Reserve_WhenQuantityIsAvailable_TracksReservedAndAvailableStock()
    {
        InventoryStock stock = StockWithOnHand(10);

        Result result = stock.Reserve(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, stock.OnHand);
        Assert.Equal(4, stock.Reserved);
        Assert.Equal(6, stock.Available);
    }

    [Fact]
    public void Reserve_WhenQuantityExceedsAvailability_ReturnsConflictWithoutMutatingStock()
    {
        InventoryStock stock = StockWithOnHand(3);

        Result result = stock.Reserve(4);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal(0, stock.Reserved);
    }

    [Fact]
    public void Release_ReopensReservedStock()
    {
        InventoryStock stock = StockWithOnHand(10);
        _ = stock.Reserve(4);

        Result result = stock.Release(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, stock.Reserved);
        Assert.Equal(9, stock.Available);
    }

    [Fact]
    public void CommitReservation_DecreasesOnHandAndReservedCounters()
    {
        InventoryStock stock = StockWithOnHand(10);
        _ = stock.Reserve(4);

        Result result = stock.CommitReservation(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, stock.OnHand);
        Assert.Equal(0, stock.Reserved);
        Assert.Equal(6, stock.Available);
    }

    [Fact]
    public void Receive_WithNonPositiveQuantity_ReturnsValidationFailure()
    {
        InventoryStock stock = new(Guid.CreateVersion7());

        Result result = stock.Receive(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    private static InventoryStock StockWithOnHand(int quantity)
    {
        InventoryStock stock = new(Guid.CreateVersion7());
        Result receiveResult = stock.Receive(quantity);
        Assert.True(receiveResult.IsSuccess);
        return stock;
    }
}
