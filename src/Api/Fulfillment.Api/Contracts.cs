using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.SharedKernel;

namespace FulfillmentPlatform.Api;

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderLineRequest>? Lines);

public sealed record CreateOrderLineRequest(Guid VariantId, int Quantity);

public sealed record CatalogItemResponse(Guid VariantId, string Sku, string Name, int Available);

public sealed record PaymentResponse(string Reference, DateTimeOffset ConfirmedAt);

public sealed record OrderConfirmedEventResponse(Guid OrderId, string PaymentReference, DateTimeOffset OccurredAt);

public sealed record OutboxMessageResponse(
    Guid Id,
    Guid OrderId,
    string Type,
    DateTimeOffset OccurredAt,
    DateTimeOffset? ProcessedAt,
    int AttemptCount,
    string? LastError);

public sealed record OrderResponse(Guid Id, Guid CustomerId, OrderStatus Status, IReadOnlyCollection<OrderLine> Lines)
{
    public static OrderResponse From(Order order) =>
        new(order.Id, order.CustomerId, order.Status, order.Lines);
}

public sealed record ProblemResponse(string Title, string Detail);

public static class ErrorHttpMapping
{
    public static IResult ToProblem(this Error error) =>
        Results.Problem(
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            },
            title: error.Code,
            detail: error.Description);
}
