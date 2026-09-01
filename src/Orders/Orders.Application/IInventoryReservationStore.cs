using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Orders.Application;

public interface IInventoryReservationStore
{
    Result Reserve(Guid variantId, int quantity);

    Result Release(Guid variantId, int quantity);
}
