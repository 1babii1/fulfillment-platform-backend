using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Catalog.Domain;

public static class CatalogErrors
{
    public static Error Required(string field) =>
        Error.Validation($"catalog.{field}.required", $"{field} is required.");

    public static Error DuplicateSku(string sku) =>
        Error.Conflict("catalog.variant.duplicate_sku", $"SKU '{sku}' already exists for this product.");

    public static Error InvalidQuantity() =>
        Error.Validation("catalog.inventory.invalid_quantity", "Quantity must be positive.");

    public static Error InsufficientStock() =>
        Error.Conflict("catalog.inventory.insufficient_stock", "The requested quantity is not available.");

    public static Error ReservationExceedsReserved() =>
        Error.Conflict("catalog.inventory.reservation_exceeds_reserved", "The requested quantity exceeds the active reservation.");
}
