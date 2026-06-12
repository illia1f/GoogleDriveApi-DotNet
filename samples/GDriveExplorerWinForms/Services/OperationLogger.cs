using System.Diagnostics;

namespace GDriveExplorerWinForms.Services;

/// <summary>
/// A single log entry produced by <see cref="OperationLogger"/> for every tracked operation.
/// </summary>
public sealed record OperationLogEntry(DateTime Timestamp, string Message, bool IsError);

/// <summary>
/// Reusable logging core: formats library calls, times them, and raises <see cref="Logged"/>
/// with the outcome. Any class that performs loggable operations takes (or exposes) one of
/// these instead of rolling its own log plumbing.
/// </summary>
public sealed class OperationLogger
{
    public event EventHandler<OperationLogEntry>? Logged;

    /// <summary>
    /// Marks a log argument that must not be quoted (e.g. a stream placeholder).
    /// </summary>
    public sealed record Verbatim(string Text);

    /// <summary>
    /// Builds the log form of a library call: strings quoted, null printed as null.
    /// </summary>
    public static string FormatCall(string method, params object?[] args) =>
        $"{method}({string.Join(", ", args.Select(arg => arg switch
        {
            null => "null",
            Verbatim verbatim => verbatim.Text,
            string s => $"\"{s}\"",
            _ => arg.ToString() ?? "null",
        }))})";

    public void Log(string message, bool isError) =>
        Logged?.Invoke(this, new OperationLogEntry(DateTime.Now, message, isError));

    /// <summary>
    /// Runs the operation and logs <paramref name="call"/> with its outcome and elapsed time.
    /// </summary>
    public Task TrackAsync(string call, Func<Task> operation, string successDetail) =>
        TrackAsync(call, async () => { await operation().ConfigureAwait(false); return true; }, _ => successDetail);

    /// <summary>
    /// Runs the operation and logs <paramref name="call"/> with its outcome and elapsed time.
    /// </summary>
    public async Task<T> TrackAsync<T>(string call, Func<Task<T>> operation, Func<T, string> successDetail)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            T result = await operation().ConfigureAwait(false);
            stopwatch.Stop();
            Log($"{call} → {successDetail(result)} ({stopwatch.ElapsedMilliseconds} ms)", isError: false);
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            Log($"{call} → cancelled ({stopwatch.ElapsedMilliseconds} ms)", isError: false);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log($"{call} → {ex.GetType().Name}: {ex.Message} ({stopwatch.ElapsedMilliseconds} ms)", isError: true);
            throw;
        }
    }
}
