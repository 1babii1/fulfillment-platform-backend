using FulfillmentPlatform.Api;
using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.Payments.Application;
using FulfillmentPlatform.SharedKernel;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<DemoCatalog>();
builder.Services.AddSingleton<IInventoryReservationStore>(serviceProvider =>
{
    DemoCatalog catalog = serviceProvider.GetRequiredService<DemoCatalog>();
    return new InMemoryInventoryReservationStore(catalog.Stocks);
});
builder.Services.AddSingleton<InMemoryOrderRepository>();
builder.Services.AddSingleton<IOrderRepository>(serviceProvider => serviceProvider.GetRequiredService<InMemoryOrderRepository>());
builder.Services.AddSingleton<InMemoryOrderEventPublisher>();
builder.Services.AddSingleton<IOrderEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<InMemoryOrderEventPublisher>());
builder.Services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
builder.Services.AddSingleton<CheckoutService>();
builder.Services.AddSingleton<ConfirmOrderPaymentService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapHealthChecks("/health").ExcludeFromDescription();

RouteGroupBuilder demo = app.MapGroup("/api/demo").WithTags("Demo");

demo.MapGet("/catalog", (DemoCatalog catalog) =>
    Results.Ok(catalog.Items.Select(item => new CatalogItemResponse(
        item.VariantId,
        item.Sku,
        item.Name,
        item.Stock.Available))));

demo.MapPost("/orders", (CreateOrderRequest request, CheckoutService checkout) =>
{
    IReadOnlyCollection<OrderLine> lines = (request.Lines ?? [])
        .Select(line => new OrderLine(line.VariantId, line.Quantity))
        .ToArray();
    Result<Order> result = checkout.Checkout(new CheckoutCommand(request.CustomerId, lines));

    return result.IsSuccess
        ? Results.Created($"/api/demo/orders/{result.Value.Id}", OrderResponse.From(result.Value))
        : result.Error!.ToProblem();
});

demo.MapGet("/orders/{orderId:guid}", (Guid orderId, IOrderRepository orders) =>
{
    Order? order = orders.Find(orderId);
    return order is null
        ? Error.NotFound("orders.order.not_found", "Order was not found.").ToProblem()
        : Results.Ok(OrderResponse.From(order));
});

demo.MapPost("/orders/{orderId:guid}/confirm-payment", (Guid orderId, ConfirmOrderPaymentService payments) =>
{
    Result<PaymentReceipt> result = payments.Confirm(orderId);

    return result.IsSuccess
        ? Results.Ok(new PaymentResponse(result.Value.Reference, result.Value.ConfirmedAt))
        : result.Error!.ToProblem();
});

demo.MapGet("/events", (InMemoryOrderEventPublisher events) =>
    Results.Ok(events.Events.Select(@event => new OrderConfirmedEventResponse(
        @event.OrderId,
        @event.PaymentReference,
        @event.OccurredAt))));

app.Run();

public partial class Program;
