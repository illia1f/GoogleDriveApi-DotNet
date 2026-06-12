namespace GDriveExplorerWinForms.Services;

/// <summary>
/// Serializes Drive operations: at most one runs at a time, each with a fresh
/// <see cref="CancellationTokenSource"/>. UI code watches <see cref="BusyChanged"/> to
/// disable commands and show progress. Errors are not surfaced here — the service
/// already logs them and the log panel is the error UX.
/// </summary>
public sealed class OperationRunner
{
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Raised when an operation starts (<c>true</c>) and when it finishes (<c>false</c>).
    /// </summary>
    public event Action<bool>? BusyChanged;

    public bool IsBusy => _cts is not null;

    /// <summary>
    /// Cancels the running operation, if any.
    /// </summary>
    public void Cancel() => _cts?.Cancel();

    /// <summary>
    /// Runs the operation unless one is already running. Returns <c>true</c> only when the
    /// operation ran to completion — <c>false</c> when skipped, cancelled or failed.
    /// </summary>
    public async Task<bool> RunAsync(Func<CancellationToken, Task> operation)
    {
        if (_cts is not null)
        {
            return false;
        }

        _cts = new CancellationTokenSource();
        BusyChanged?.Invoke(true);
        try
        {
            await operation(_cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            // logged by the service as "cancelled"
            return false;
        }
        catch (Exception)
        {
            // logged by the service with full details; the red log entry is the error UX
            return false;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            BusyChanged?.Invoke(false);
        }
    }
}
