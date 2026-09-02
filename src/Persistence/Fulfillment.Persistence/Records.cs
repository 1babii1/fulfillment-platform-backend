using FulfillmentPlatform.Orders.Domain;

namespace FulfillmentPlatform.Persistence;

public sealed class InventoryRecord
{
    public Guid VariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int OnHand { get; set; }

    public int Reserved { get; set; }
}

public sealed class OrderRecord
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public OrderStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderLineRecord> Lines { get; set; } = [];
}

public sealed class OrderLineRecord
{
    public long Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid VariantId { get; set; }

    public int Quantity { get; set; }
}

public sealed class OutboxMessageRecord
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}

public sealed class IdempotencyRecord
{
    public string Operation { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? ResponseStatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
