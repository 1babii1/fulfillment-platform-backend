using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fulfillment.Api.IntegrationTests;

public sealed class DemoCheckoutFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DemoCheckoutFlowTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Catalog_ReturnsAvailableDemoItems()
    {
        JsonElement[]? catalog = await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/catalog");

        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog);
        Assert.All(catalog, item => Assert.True(item.GetProperty("available").GetInt32() > 0));
    }

    [Fact]
    public async Task CheckoutAndPaymentConfirmation_ConfirmOrderAndPublishEvent()
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

        JsonElement[] events = (await _client.GetFromJsonAsync<JsonElement[]>("/api/demo/events"))!;
        Assert.Contains(events, @event => @event.GetProperty("orderId").GetGuid() == orderId);
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
}
