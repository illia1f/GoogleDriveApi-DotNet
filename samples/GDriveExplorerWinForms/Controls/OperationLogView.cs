using GDriveExplorerWinForms.Services;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// Read-only, color-coded view of <see cref="OperationLogger.Logged"/> entries.
/// Subscribes on construction and marshals entries to the UI thread itself, so the host
/// only has to place it in the layout.
/// </summary>
public sealed class OperationLogView : RichTextBox
{
    private readonly OperationLogger _logger;

    public OperationLogView(OperationLogger logger)
    {
        _logger = logger;
        ReadOnly = true;
        BorderStyle = BorderStyle.None;
        BackColor = Theme.SurfaceAlt;
        Font = Theme.MonoFont;
        ForeColor = Theme.TextPrimary;

        _logger.Logged += OnOperationLogged;
    }

    private void OnOperationLogged(object? sender, OperationLogEntry entry)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendEntry(entry));
        }
        else
        {
            AppendEntry(entry);
        }
    }

    private void AppendEntry(OperationLogEntry entry)
    {
        SelectionStart = TextLength;
        SelectionColor = Theme.TextSecondary;
        AppendText($"[{entry.Timestamp:HH:mm:ss}] ");
        SelectionColor = entry.IsError ? Theme.Error : Theme.TextPrimary;
        AppendText(entry.Message + Environment.NewLine);
        ScrollToCaret();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.Logged -= OnOperationLogged;
        }

        base.Dispose(disposing);
    }
}
