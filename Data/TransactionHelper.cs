using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace erp.Data;

/// <summary>
/// Helper for managing database transactions with consistent behavior.
/// Ensures proper commit/rollback and resource cleanup.
/// </summary>
public static class TransactionHelper
{
    /// <summary>
    /// Executes an operation within a transaction scope.
    /// The transaction is committed only if the operation completes successfully.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        ApplicationDbContext context,
        Func<IDbContextTransaction, Task<T>> operation)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var result = await operation(transaction);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Debug.WriteLine($"Transaction rolled back: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within a transaction scope (no return value).
    /// The transaction is committed only if the operation completes successfully.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="operation">The operation to execute within the transaction.</param>
    public static async Task ExecuteInTransactionAsync(
        ApplicationDbContext context,
        Func<IDbContextTransaction, Task> operation)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await operation(transaction);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Debug.WriteLine($"Transaction rolled back: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Executes an operation within a transaction scope with retry policy.
    /// Useful for handling transient failures and deadlocks.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="operation">The operation to execute within the transaction.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="delayMs">Delay between retries in milliseconds (default: 100).</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> ExecuteInTransactionWithRetryAsync<T>(
        ApplicationDbContext context,
        Func<IDbContextTransaction, Task<T>> operation,
        int maxRetries = 3,
        int delayMs = 100)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ExecuteInTransactionAsync(context, operation);
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                await Task.Delay(delayMs * (attempt + 1));
            }
        }

        // Last attempt - let exceptions propagate
        return await ExecuteInTransactionAsync(context, operation);
    }

    /// <summary>
    /// Determines if an exception is a transient error that can be retried.
    /// </summary>
    private static bool IsTransientError(Exception ex)
    {
        // PostgreSQL error codes for transient failures
        // 40001: serialization_failure (deadlock)
        // 53000: insufficient_resources
        // 53100: disk_full
        // 53200: out_of_memory
        // 53300: too_many_connections
        // 55P03: lock_not_available
        // 57P03: cannot_connect_now
        // 58P01: system_error
        string? message = ex.Message.ToLowerInvariant();
        return message.Contains("deadlock") ||
               message.Contains("serialization") ||
               message.Contains("lock") ||
               ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException;
    }
}
