using FulfillmentPlatform.Api;
using FulfillmentPlatform.Catalog.Application;
using FulfillmentPlatform.Catalog.Domain;
using FulfillmentPlatform.Orders.Application;
using FulfillmentPlatform.Orders.Domain;
using FulfillmentPlatform.Payments.Application;
using FulfillmentPlatform.Persistence;
using FulfillmentPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FulfillmentDbContext>("postgres", tags: ["ready"]);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

bool hasOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("fulfillment-platform-backend"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation(options => options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/health"))
            .AddSource("Npgsql")
            .AddSource(OutboxPublisher.ActivitySourceName);
        if (hasOtlpExporter)
        {
            tracing.AddOtlpExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation();
        if (hasOtlpExporter)
        {
            metrics.AddOtlpExporter();
        }
    })
    .WithLogging(logging =>
    {
        if (hasOtlpExporter)
        {
            logging.AddOtlpExporter();
        }
    });

string connectionString = builder.Configuration.GetConnectionString("FulfillmentDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:FulfillmentDatabase must be configured.");

builder.Services.AddDbContext<FulfillmentDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ICatalogReadStore, EfCatalogReadStore>();
builder.Services.AddScoped<IInventoryReservationStore, EfInventoryReservationStore>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderLockingExecutor, EfOrderLockingExecutor>();
builder.Services.AddScoped<DatabaseTransactionExecutor>();
builder.Services.AddScoped<IOrderEventPublisher, EfOutboxEventPublisher>();
builder.Services.AddSingleton<InMemoryOutboxTransport>();
builder.Services.AddSingleton<IOutboxTransport>(serviceProvider => serviceProvider.GetRequiredService<InMemoryOutboxTransport>());
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<ConfirmOrderPaymentService>();
builder.Services.AddScoped<IdempotencyExecutor>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    DatabaseInitializer.Initialize(scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>());
}

app.UseExceptionHandler();
app.MapOpenApi();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).ExcludeFromDescription();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).ExcludeFromDescription();

RouteGroupBuilder demo = app.MapGroup("/api/demo").WithTags("Demo");

demo.MapGet("/catalog", (ICatalogReadStore catalog) =>
    Results.Ok(catalog.GetAvailableItems().Select(item => new CatalogItemResponse(
        item.VariantId,
        item.Sku,
        item.Name,
        item.Available))));

demo.MapPost("/orders", (CreateOrderRequest request, HttpRequest httpRequest, CheckoutService checkout, IdempotencyExecutor idempotency, DatabaseTransactionExecutor transactions) =>
{
    IdempotentHttpResponse HandleCheckout()
    {
        IReadOnlyCollection<OrderLine> lines = (request.Lines ?? [])
            .Select(line => new OrderLine(line.VariantId, line.Quantity))
            .ToArray();
        Result<Order> result = checkout.Checkout(new CheckoutCommand(request.CustomerId, lines));

        return result.IsSuccess
            ? new IdempotentHttpResponse(StatusCodes.Status201Created, OrderResponse.From(result.Value))
            : new IdempotentHttpResponse(result.Error!.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            }, new ProblemResponse(result.Error.Code, result.Error.Description));
    }

    string? key = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
    if (key is not null)
    {
        return idempotency.Execute("checkout", key, request, HandleCheckout);
    }

    IdempotentHttpResponse response = transactions.Execute(HandleCheckout, value => value.StatusCode < StatusCodes.Status400BadRequest);
    return Results.Json(response.Body, statusCode: response.StatusCode);
});

demo.MapGet("/orders/{orderId:guid}", (Guid orderId, IOrderRepository orders) =>
{
    Order? order = orders.Find(orderId);
    return order is null
        ? Error.NotFound("orders.order.not_found", "Order was not found.").ToProblem()
        : Results.Ok(OrderResponse.From(order));
});

demo.MapPost("/orders/{orderId:guid}/confirm-payment", (Guid orderId, HttpRequest httpRequest, ConfirmOrderPaymentService payments, IdempotencyExecutor idempotency) =>
{
    IdempotentHttpResponse HandleConfirmation()
    {
        Result<PaymentReceipt> result = payments.Confirm(orderId);
        return result.IsSuccess
            ? new IdempotentHttpResponse(StatusCodes.Status200OK, new PaymentResponse(result.Value.Reference, result.Value.ConfirmedAt))
            : new IdempotentHttpResponse(result.Error!.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            }, new ProblemResponse(result.Error.Code, result.Error.Description));
    }

    string? key = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
    if (key is not null)
    {
        return idempotency.Execute("payment-confirmation", key, new { orderId }, HandleConfirmation);
    }

    IdempotentHttpResponse response = HandleConfirmation();
    return Results.Json(response.Body, statusCode: response.StatusCode);
});

demo.MapGet("/outbox", async (FulfillmentDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.OutboxMessages
        .AsNoTracking()
        .OrderBy(message => message.OccurredAt)
        .Select(message => new OutboxMessageResponse(
            message.Id,
            message.OrderId,
            message.Type,
            message.OccurredAt,
            message.ProcessedAt,
            message.AttemptCount))
        .ToArrayAsync(cancellationToken)));

demo.MapGet("/events", (InMemoryOutboxTransport transport) =>
    Results.Ok(transport.Published.Select(message => new OutboxMessageResponse(
        message.Id,
        message.OrderId,
        message.Type,
        message.OccurredAt,
        ProcessedAt: null,
        AttemptCount: 1))));

app.Run();

public partial class Program;
