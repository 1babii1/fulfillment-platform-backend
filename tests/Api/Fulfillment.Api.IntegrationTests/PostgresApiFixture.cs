using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Fulfillment.Api.IntegrationTests;

public sealed class PostgresApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17.5-alpine")
        .WithDatabase("fulfillment_tests")
        .WithUsername("test_runner")
        .WithPassword(Guid.CreateVersion7().ToString("N"))
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    public async Task DisposeAsync() => await _database.DisposeAsync();

    public WebApplicationFactory<Program> CreateApi(IReadOnlyDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:FulfillmentDatabase", _database.GetConnectionString());
                if (settings is null)
                {
                    return;
                }

                foreach ((string key, string? value) in settings)
                {
                    builder.UseSetting(key, value);
                }
            });
}
