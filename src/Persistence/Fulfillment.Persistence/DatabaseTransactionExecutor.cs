namespace FulfillmentPlatform.Persistence;

public sealed class DatabaseTransactionExecutor(FulfillmentDbContext db)
{
    public T Execute<T>(Func<T> action, Func<T, bool> shouldCommit)
    {
        using var transaction = db.Database.CurrentTransaction is null
            ? db.Database.BeginTransaction()
            : null;

        try
        {
            T result = action();
            if (transaction is not null)
            {
                if (shouldCommit(result))
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
            }

            return result;
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
    }
}
