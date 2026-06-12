using GDriveExplorerWinForms.Controls;
using GDriveExplorerWinForms.Services;
using GoogleDriveApi_DotNet;

namespace GDriveExplorerWinForms;

/// <summary>
/// Application shell. Shows the <see cref="ConnectPanel"/> first; once OAuth succeeds it swaps
/// in the <see cref="ExplorerPanel"/>. Owns (and disposes) the single <see cref="GoogleDriveApi"/>
/// instance for the whole app lifetime.
/// </summary>
public sealed class MainForm : Form
{
    private DriveExplorerService? _service;

    public MainForm()
    {
        Text = "Google Drive Explorer — GoogleDriveApi-DotNet sample";
        Font = Theme.BaseFont;
        BackColor = Theme.Surface;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 720);

        var connectPanel = new ConnectPanel { Dock = DockStyle.Fill };
        connectPanel.Connected += OnConnected;
        Controls.Add(connectPanel);
    }

    private void OnConnected(object? sender, GoogleDriveApi api)
    {
        // Defer the swap: this handler runs on ConnectPanel's stack, and disposing the panel
        // here would dispose the control that is still raising the event.
        BeginInvoke(() => SwapToExplorer(api));
    }

    private void SwapToExplorer(GoogleDriveApi api)
    {
        _service = new DriveExplorerService(api);

        // Controls.Clear() only detaches child controls — it never disposes them.
        Control[] oldControls = [.. Controls.Cast<Control>()];
        Controls.Clear();
        foreach (Control control in oldControls)
        {
            control.Dispose();
        }

        var explorer = new ExplorerPanel(_service) { Dock = DockStyle.Fill };
        Controls.Add(explorer);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _service?.Dispose();
        }

        base.Dispose(disposing);
    }
}
