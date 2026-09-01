using FulfillmentPlatform.Api;
using FulfillmentPlatform.Catalog.Application;
using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.Payments.Application;
using FulfillmentPlatform.Persistence;
using FulfillmentPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

string connectionString = builder.Configuration.GetConnectionString("FulfillmentDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:FulfillmentDatabase must be configured.");

builder.Services.AddDbContext<FulfillmentDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICatalogReadStore, EfCatalogReadStore>();
builder.Services.AddScoped<IInventoryReservationStore, EfInventoryReservationStore>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddSingleton<InMemoryOrderEventPublisher>();
builder.Services.AddSingleton<IOrderEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<InMemoryOrderEventPublisher>());
builder.Services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<ConfirmOrderPaymentService>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    DatabaseInitializer.Initialize(scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>());
}

app.UseExceptionHandler();
app.MapOpenApi();
app.MapHealthChecks("/health").ExcludeFromDescription();

RouteGroupBuilder demo = app.MapGroup("/api/demo").WithTags("Demo");

demo.MapGet("/catalog", (ICatalogReadStore catalog) =>
    Results.Ok(catalog.GetAvailableItems().Select(item => new CatalogItemResponse(
        item.VariantId,
        item.Sku,
        item.Name,
        item.Available))));

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
