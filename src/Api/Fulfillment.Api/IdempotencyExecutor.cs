using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FulfillmentPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Api;

public sealed class IdempotencyExecutor(FulfillmentDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public IResult Execute(string operation, string key, object request, Func<IdempotentHttpResponse> action)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "idempotency.invalid_key");
        }

        string requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions))));
        using var transaction = db.Database.BeginTransaction();
        int claimed = db.Database.ExecuteSqlInterpolated($"""
            INSERT INTO idempotency_records (operation, key, request_hash, status, created_at)
            VALUES ({operation}, {key}, {requestHash}, {"Completed"}, {DateTimeOffset.UtcNow})
            ON CONFLICT (operation, key) DO NOTHING
            """);

        if (claimed == 0)
        {
            IdempotencyRecord existing = db.IdempotencyRecords
                .AsNoTracking()
                .Single(record => record.Operation == operation && record.Key == key);
            transaction.Commit();

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "idempotency.key_reused");
            }

            return Results.Content(existing.ResponseBody!, "application/json", statusCode: existing.ResponseStatusCode);
        }

        IdempotentHttpResponse response = action();
        IdempotencyRecord record = db.IdempotencyRecords.Single(item => item.Operation == operation && item.Key == key);
        record.ResponseStatusCode = response.StatusCode;
        record.ResponseBody = JsonSerializer.Serialize(response.Body, JsonOptions);
        db.SaveChanges();
        transaction.Commit();

        return Results.Content(record.ResponseBody, "application/json", statusCode: response.StatusCode);
    }
}

public sealed record IdempotentHttpResponse(int StatusCode, object Body);
