using GDriveExplorerWinForms.Services;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// Status bar with auth state, current folder and a token-freshness dot that re-checks
/// itself every 30 s. Clicking the dot raises <see cref="TokenRefreshRequested"/>; the host
/// runs the actual <c>TryRefreshTokenAsync</c> call and decides the busy policy.
/// </summary>
public sealed class ExplorerStatusBar : StatusStrip
{
    private readonly DriveExplorerService _service;
    private readonly Func<(string name, string id)> _currentFolder;
    private readonly ToolStripStatusLabel _authLabel;
    private readonly ToolStripStatusLabel _folderLabel;
    private readonly ToolStripStatusLabel _tokenDot;
    private readonly ToolStripProgressBar _progressBar;
    private readonly System.Windows.Forms.Timer _tokenTimer;

    public event EventHandler? TokenRefreshRequested;

    public ExplorerStatusBar(DriveExplorerService service, Func<(string name, string id)> currentFolder)
    {
        _service = service;
        _currentFolder = currentFolder;

        BackColor = Theme.SurfaceAlt;
        SizingGrip = false;
        Font = Theme.SmallFont;

        _authLabel = new ToolStripStatusLabel { ForeColor = Theme.TextSecondary };
        _folderLabel = new ToolStripStatusLabel { ForeColor = Theme.TextSecondary, Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _tokenDot = new ToolStripStatusLabel(Icons.StatusDot);
        _tokenDot.Click += (_, _) => TokenRefreshRequested?.Invoke(this, EventArgs.Empty);
        _progressBar = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, Visible = false, Width = 120 };
        Items.AddRange([_authLabel, _folderLabel, _tokenDot, _progressBar]);

        _tokenTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _tokenTimer.Tick += (_, _) => RefreshStatus();
        _tokenTimer.Start();

        RefreshStatus();
    }

    /// <summary>
    /// Shows/hides the marquee progress bar.
    /// </summary>
    public void SetBusy(bool busy) => _progressBar.Visible = busy;

    public void RefreshStatus()
    {
        (string folderName, string folderId) = _currentFolder();
        _authLabel.Text = _service.IsAuthorized
            ? $"Connected ({_service.ApplicationName}) | root: {_service.RootFolderId}"
            : "Not authorized";
        _folderLabel.Text = $"Current folder: {folderName} ({folderId})";
        bool stale = _service.IsTokenStale;
        _tokenDot.ForeColor = stale ? Theme.Warning : Theme.Success;
        _tokenDot.ToolTipText = stale
            ? "Token is stale (IsTokenShouldBeRefreshed = true). Click to call TryRefreshTokenAsync()."
            : "Token is fresh (IsTokenShouldBeRefreshed = false). Click to call TryRefreshTokenAsync() anyway.";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tokenTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
