using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fulfillment.Api.IntegrationTests;

public sealed class DemoCheckoutFlowTests : IClassFixture<PostgresApiFixture>, IDisposable
{
    private readonly PostgresApiFixture _database;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DemoCheckoutFlowTests(PostgresApiFixture database)
    {
        _database = database;
        _factory = database.CreateApi();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Catalog_ReturnsAvailableDemoItems()
    {
        JsonElement[]? catalog = await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog");

        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog);
        Assert.All(catalog, item => Assert.True(item.GetProperty("available").GetInt32() > 0));
    }

    [Fact]
    public async Task HealthEndpoints_ReportLiveProcessAndReadyDatabase()
    {
        using HttpResponseMessage liveness = await _client.GetAsync("/health/live");
        using HttpResponseMessage readiness = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
    }

    [Fact]
    public async Task CheckoutAndPaymentConfirmation_ConfirmsOrderAndStoresDurableEvent()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        Guid variantId = catalog[0].GetProperty("variantId").GetGuid();

        HttpResponseMessage checkoutResponse = await _client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[] { new { variantId, quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        JsonElement checkout = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Guid orderId = checkout.GetProperty("id").GetGuid();
        Assert.Equal("PendingPayment", checkout.GetProperty("status").GetString());

        HttpResponseMessage paymentResponse = await _client.PostAsync($"/api/demo/orders/{orderId}/confirm-payment", null);
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);

        JsonElement order = (await _client.GetFromJsonAsync<JsonElement>($"/api/demo/orders/{orderId}"))!;
        Assert.Equal("Confirmed", order.GetProperty("status").GetString());

        JsonElement[] messages = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/outbox"))!;
        Assert.Contains(messages, message => message.GetProperty("orderId").GetGuid() == orderId);
    }

    [Fact]
    public async Task Checkout_WithNoLines_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = (await response.Content.ReadFromJsonAsync<JsonElement>())!;
        Assert.Equal("orders.lines.empty", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Order_PersistsAcrossApiHostRestart()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        Guid variantId = catalog[0].GetProperty("variantId").GetGuid();

        HttpResponseMessage checkoutResponse = await _client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[] { new { variantId, quantity = 1 } }
        });
        JsonElement checkout = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Guid orderId = checkout.GetProperty("id").GetGuid();

        _client.Dispose();
        _factory.Dispose();
        using WebApplicationFactory<Program> restartedApi = _database.CreateApi();
        using HttpClient restartedClient = restartedApi.CreateClient();
        JsonElement persistedOrder = (await restartedClient.GetFromJsonAsync<JsonElement>($"/api/demo/orders/{orderId}"))!;

        Assert.Equal(orderId, persistedOrder.GetProperty("id").GetGuid());
        Assert.Equal("PendingPayment", persistedOrder.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ParallelCheckoutRequests_DoNotOversellInventory()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        JsonElement item = catalog[0];
        Guid variantId = item.GetProperty("variantId").GetGuid();
        int availableBeforeCheckout = item.GetProperty("available").GetInt32();

        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, availableBeforeCheckout + 5)
            .Select(_ => _client.PostAsJsonAsync("/api/demo/orders", new
            {
                customerId = Guid.CreateVersion7(),
                lines = new[] { new { variantId, quantity = 1 } }
            }))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        int successfulReservations = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }

        JsonElement[] catalogAfterCheckout = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        JsonElement itemAfterCheckout = catalogAfterCheckout
            .SingleOrDefault(catalogItem => catalogItem.GetProperty("variantId").GetGuid() == variantId);
        int availableAfterCheckout = itemAfterCheckout.ValueKind == JsonValueKind.Undefined
            ? 0
            : itemAfterCheckout.GetProperty("available").GetInt32();

        Assert.Equal(availableBeforeCheckout, successfulReservations);
        Assert.Equal(0, availableAfterCheckout);
    }

    [Fact]
    public async Task Checkout_WhenLaterLineExceedsStock_ReleasesEarlierReservation()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        JsonElement availableItem = catalog[0];
        JsonElement insufficientItem = catalog[1];
        Guid availableVariantId = availableItem.GetProperty("variantId").GetGuid();
        Guid insufficientVariantId = insufficientItem.GetProperty("variantId").GetGuid();
        int availableBeforeCheckout = availableItem.GetProperty("available").GetInt32();
        int insufficientAvailable = insufficientItem.GetProperty("available").GetInt32();

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[]
            {
                new { variantId = availableVariantId, quantity = 1 },
                new { variantId = insufficientVariantId, quantity = insufficientAvailable + 1 }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement[] catalogAfterCheckout = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        int availableAfterCheckout = catalogAfterCheckout
            .Single(catalogItem => catalogItem.GetProperty("variantId").GetGuid() == availableVariantId)
            .GetProperty("available")
            .GetInt32();

        Assert.Equal(availableBeforeCheckout, availableAfterCheckout);
    }

    [Fact]
    public async Task PaymentConfirmation_PersistsAnOutboxMessage()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        Guid variantId = catalog[0].GetProperty("variantId").GetGuid();
        HttpResponseMessage checkoutResponse = await _client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[] { new { variantId, quantity = 1 } }
        });
        JsonElement checkout = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Guid orderId = checkout.GetProperty("id").GetGuid();

        HttpResponseMessage paymentResponse = await _client.PostAsync($"/api/demo/orders/{orderId}/confirm-payment", null);
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);

        HttpResponseMessage outboxResponse = await _client.GetAsync("/api/demo/outbox");
        Assert.Equal(HttpStatusCode.OK, outboxResponse.StatusCode);
        JsonElement[] messages = (await outboxResponse.Content.ReadFromJsonAsync<JsonElement[]>())!;
        Assert.Contains(messages, message => message.GetProperty("orderId").GetGuid() == orderId);
    }

    [Fact]
    public async Task OutboxPublisher_DeliversMessageAndMarksItProcessed()
    {
        Guid orderId = await CreateAndConfirmOrderAsync(_client);

        JsonElement[] messages = await WaitForOutboxAsync(
            _client,
            values => values.Any(message =>
                message.GetProperty("orderId").GetGuid() == orderId &&
                message.GetProperty("processedAt").ValueKind == JsonValueKind.String));

        Assert.Contains(messages, message => message.GetProperty("orderId").GetGuid() == orderId);
        JsonElement[] published = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/events"))!;
        Assert.Contains(published, message => message.GetProperty("orderId").GetGuid() == orderId);
    }

    [Fact]
    public async Task Checkout_WithSameIdempotencyKey_ReturnsOriginalOrderWithoutSecondReservation()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        JsonElement item = catalog[0];
        Guid variantId = item.GetProperty("variantId").GetGuid();
        int availableBefore = item.GetProperty("available").GetInt32();
        Guid customerId = Guid.CreateVersion7();

        using HttpRequestMessage firstRequest = CreateCheckoutRequest(customerId, variantId, 1, "checkout-retry-001");
        using HttpResponseMessage firstResponse = await _client.SendAsync(firstRequest);
        using HttpRequestMessage retryRequest = CreateCheckoutRequest(customerId, variantId, 1, "checkout-retry-001");
        using HttpResponseMessage retryResponse = await _client.SendAsync(retryRequest);

        JsonElement first = (await firstResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        JsonElement retry = (await retryResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
        Assert.Equal(first.GetProperty("id").GetGuid(), retry.GetProperty("id").GetGuid());

        JsonElement[] after = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        int availableAfter = after.Single(catalogItem => catalogItem.GetProperty("variantId").GetGuid() == variantId)
            .GetProperty("available")
            .GetInt32();
        Assert.Equal(availableBefore - 1, availableAfter);
    }

    [Fact]
    public async Task ConcurrentCheckout_WithSameIdempotencyKey_CreatesOneOrder()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        JsonElement item = catalog[0];
        Guid variantId = item.GetProperty("variantId").GetGuid();
        int availableBefore = item.GetProperty("available").GetInt32();
        Guid customerId = Guid.CreateVersion7();

        Task<HttpResponseMessage>[] requests = Enumerable.Range(0, 4)
            .Select(async _ =>
            {
                using HttpRequestMessage request = CreateCheckoutRequest(customerId, variantId, 1, "checkout-concurrent-001");
                return await _client.SendAsync(request);
            })
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(requests);
        Guid[] orderIds = [];
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            orderIds = (await Task.WhenAll(responses.Select(async response =>
                (await response.Content.ReadFromJsonAsync<JsonElement>())!.GetProperty("id").GetGuid()))).ToArray();
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }

        Assert.Single(orderIds.Distinct());
        JsonElement[] after = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        int availableAfter = after.Single(catalogItem => catalogItem.GetProperty("variantId").GetGuid() == variantId)
            .GetProperty("available")
            .GetInt32();
        Assert.Equal(availableBefore - 1, availableAfter);
    }

    [Fact]
    public async Task Checkout_WithReusedIdempotencyKeyAndDifferentPayload_ReturnsConflict()
    {
        JsonElement[] catalog = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        Guid variantId = catalog[0].GetProperty("variantId").GetGuid();
        Guid customerId = Guid.CreateVersion7();

        using HttpRequestMessage firstRequest = CreateCheckoutRequest(customerId, variantId, 1, "checkout-reuse-001");
        using HttpResponseMessage firstResponse = await _client.SendAsync(firstRequest);
        using HttpRequestMessage conflictingRequest = CreateCheckoutRequest(customerId, variantId, 2, "checkout-reuse-001");
        using HttpResponseMessage conflictingResponse = await _client.SendAsync(conflictingRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictingResponse.StatusCode);
    }

    [Fact]
    public async Task PaymentConfirmation_WithSameIdempotencyKey_ReturnsOriginalReceipt()
    {
        Guid orderId = await CreateOrderAsync(_client);

        using HttpRequestMessage firstRequest = CreatePaymentConfirmationRequest(orderId, "payment-retry-001");
        using HttpResponseMessage firstResponse = await _client.SendAsync(firstRequest);
        using HttpRequestMessage retryRequest = CreatePaymentConfirmationRequest(orderId, "payment-retry-001");
        using HttpResponseMessage retryResponse = await _client.SendAsync(retryRequest);

        JsonElement first = (await firstResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        JsonElement retry = (await retryResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.Equal(first.GetProperty("reference").GetString(), retry.GetProperty("reference").GetString());
    }

    private static HttpRequestMessage CreateCheckoutRequest(Guid customerId, Guid variantId, int quantity, string idempotencyKey)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/demo/orders")
        {
            Content = JsonContent.Create(new
            {
                customerId,
                lines = new[] { new { variantId, quantity } }
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static HttpRequestMessage CreatePaymentConfirmationRequest(Guid orderId, string idempotencyKey)
    {
        HttpRequestMessage request = new(HttpMethod.Post, $"/api/demo/orders/{orderId}/confirm-payment");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static async Task<Guid> CreateAndConfirmOrderAsync(HttpClient client)
    {
        Guid orderId = await CreateOrderAsync(client);
        HttpResponseMessage paymentResponse = await client.PostAsync($"/api/demo/orders/{orderId}/confirm-payment", null);
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);
        return orderId;
    }

    private static async Task<Guid> CreateOrderAsync(HttpClient client)
    {
        JsonElement[] catalog = (await client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog"))!;
        HttpResponseMessage checkoutResponse = await client.PostAsJsonAsync("/api/demo/orders", new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[] { new { variantId = catalog[0].GetProperty("variantId").GetGuid(), quantity = 1 } }
        });
        JsonElement checkout = (await checkoutResponse.Content.ReadFromJsonAsync<JsonElement>())!;
        Guid orderId = checkout.GetProperty("id").GetGuid();
        return orderId;
    }

    private static async Task<JsonElement[]> WaitForOutboxAsync(HttpClient client, Func<JsonElement[], bool> condition)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            JsonElement[] messages = (await client.GetFromJsonAsync<JsonElement[]>("/api/demo/outbox"))!;
            if (condition(messages))
            {
                return messages;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return (await client.GetFromJsonAsync<JsonElement[]>("/api/demo/outbox"))!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
