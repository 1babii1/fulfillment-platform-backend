using Microsoft.Extensions.Configuration;

namespace FulfillmentPlatform.Payments.Application;

public sealed class InMemoryOutboxTransport(IConfiguration configuration) : IOutboxTransport
{
    private readonly List<OutboxEnvelope> _published = [];
    private readonly Lock _lock = new();

    public IReadOnlyCollection<OutboxEnvelope> Published
    {
        get
        {
            lock (_lock)
            {
                return _published.ToArray();
            }
        }
    }

    public Task PublishAsync(OutboxEnvelope message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (configuration.GetValue<bool>("DemoOutbox:FailPublishing"))
        {
            throw new InvalidOperationException("Demo outbox transport is configured to fail.");
        }

        lock (_lock)
        {
            _published.Add(message);
        }

        return Task.CompletedTask;
    }
}
