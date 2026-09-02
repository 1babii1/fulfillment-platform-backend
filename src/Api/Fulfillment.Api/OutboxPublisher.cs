using FulfillmentPlatform.Payments.Application;
using FulfillmentPlatform.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FulfillmentPlatform.Api;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    public const string ActivitySourceName = "FulfillmentPlatform.Outbox";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(250);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int processed = await PublishBatchAsync(stoppingToken);
            if (processed == 0)
            {
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        int processed = 0;

        for (int index = 0; index < BatchSize; index++)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            FulfillmentDbContext db = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
            IOutboxTransport transport = scope.ServiceProvider.GetRequiredService<IOutboxTransport>();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            OutboxMessageRecord? message = await db.OutboxMessages
                .FromSqlRaw("""
                    SELECT *
                    FROM outbox_messages
                    WHERE processed_at IS NULL
                    ORDER BY occurred_at
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (message is null)
            {
                await transaction.CommitAsync(cancellationToken);
                break;
            }

            message.AttemptCount++;
            using Activity? activity = ActivitySource.StartActivity("outbox.publish");
            activity?.SetTag("outbox.message_type", message.Type);
            activity?.SetTag("outbox.attempt", message.AttemptCount);
            try
            {
                await transport.PublishAsync(
                    new OutboxEnvelope(message.Id, message.OrderId, message.Type, message.Payload, message.OccurredAt),
                    cancellationToken);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                activity?.SetStatus(ActivityStatusCode.Ok);
                message.LastError = null;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                processed++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "transport failure");
                message.LastError = exception.Message[..Math.Min(exception.Message.Length, 2048)];
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning(exception, "Outbox message {OutboxMessageId} publishing failed on attempt {AttemptCount}", message.Id, message.AttemptCount);
                break;
            }
        }

        return processed;
    }
}
