using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Orders.Domain;

public static class OrderErrors
{
    public static Error EmptyOrder() =>
        Error.Validation("orders.lines.empty", "An order must contain at least one line.");

    public static Error InvalidQuantity() =>
        Error.Validation("orders.lines.invalid_quantity", "Order line quantity must be positive.");

    public static Error DuplicateVariant() =>
        Error.Validation("orders.lines.duplicate_variant", "Each variant may appear only once in an order.");

    public static Error InvalidTransition(OrderStatus currentStatus, OrderStatus targetStatus) =>
        Error.Conflict(
            "orders.status.invalid_transition",
            $"Cannot transition an order from {currentStatus} to {targetStatus}.");
}
