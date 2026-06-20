using GoogleDriveApi_DotNet;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// Initial screen: lets the user pick credentials.json, a token folder and an application name,
/// then builds and authorizes a <see cref="GoogleDriveApi"/> instance via the fluent builder.
/// </summary>
public sealed class ConnectPanel : UserControl
{
    private readonly TextBox _credentialsPathBox;
    private readonly TextBox _tokenFolderBox;
    private readonly TextBox _appNameBox;
    private readonly Button _connectButton;
    private readonly Label _errorLabel;

    /// <summary>
    /// Raised after a successful <c>BuildAsync</c> (OAuth completed).
    /// </summary>
    public event EventHandler<GoogleDriveApi>? Connected;

    public ConnectPanel()
    {
        BackColor = Theme.SurfaceAlt;

        var card = new Panel
        {
            Size = new Size(460, 440),
            BackColor = Theme.Surface,
            Anchor = AnchorStyles.None,
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 20, 28, 12),
            ColumnCount = 2,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddSpanningRow(grid, new Label
        {
            Text = "Connect to Google Drive",
            Font = Theme.TitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Height = 40,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
        });

        AddSpanningRow(grid, new Label
        {
            Text = "Uses GoogleDriveApi.CreateBuilder() — pick your OAuth client credentials\n" +
                   "(see docs/getting-started.md) and press Connect.",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Height = 40,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(3, 0, 3, 10),
        });

        _credentialsPathBox = AddPathRow(grid, "credentials.json path", "credentials.json", BrowseCredentials);
        _tokenFolderBox = AddPathRow(grid, "Token folder (token cache)", "_metadata", BrowseTokenFolder);

        AddSpanningRow(grid, MakeFieldLabel("Application name"));
        _appNameBox = new TextBox
        {
            Text = "QuickFilesLoad",
            Font = Theme.BaseFont,
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3, 2, 3, 8),
        };
        AddSpanningRow(grid, _appNameBox);

        _connectButton = new Button
        {
            Text = "Connect",
            Height = 38,
            Dock = DockStyle.Top,
            Margin = new Padding(3, 10, 3, 0),
        };
        Theme.StylePrimaryButton(_connectButton);
        _connectButton.Click += OnConnectClicked;
        AddSpanningRow(grid, _connectButton);

        // The error label takes the remaining card height so long exception messages have room.
        _errorLabel = new Label
        {
            ForeColor = Theme.Error,
            Font = Theme.SmallFont,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        int errorRow = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        grid.Controls.Add(_errorLabel, 0, errorRow);
        grid.SetColumnSpan(_errorLabel, 2);

        card.Controls.Add(grid);
        Controls.Add(card);
        Resize += (_, _) => card.Location = new Point((Width - card.Width) / 2, (Height - card.Height) / 2);
    }

    private static void AddSpanningRow(TableLayoutPanel grid, Control control)
    {
        int row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(control, 0, row);
        grid.SetColumnSpan(control, 2);
    }

    private TextBox AddPathRow(TableLayoutPanel grid, string labelText, string defaultValue, Action<TextBox> browse)
    {
        AddSpanningRow(grid, MakeFieldLabel(labelText));

        var box = new TextBox
        {
            Text = defaultValue,
            Font = Theme.BaseFont,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3, 2, 6, 8),
        };

        var browseButton = new Button
        {
            Text = "…",
            Size = new Size(62, 27),
            Margin = new Padding(0, 2, 3, 8),
        };
        Theme.StyleSecondaryButton(browseButton);
        browseButton.Click += (_, _) => browse(box);

        int row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(box, 0, row);
        grid.Controls.Add(browseButton, 1, row);
        return box;
    }

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        Font = Theme.SmallFont,
        ForeColor = Theme.TextSecondary,
        AutoSize = true,
        Margin = new Padding(3, 0, 3, 2),
    };

    private static void BrowseCredentials(TextBox target)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select credentials.json",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            target.Text = dialog.FileName;
        }
    }

    private static void BrowseTokenFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select folder for the OAuth token cache" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
        _errorLabel.Text = string.Empty;

        if (!File.Exists(_credentialsPathBox.Text))
        {
            _errorLabel.Text = "credentials.json not found at the given path.";
            return;
        }

        _connectButton.Enabled = false;
        _connectButton.Text = "Waiting for Google sign-in…";
        try
        {
            // The fluent builder is the library's single entry point; BuildAsync runs the
            // interactive OAuth flow (browser window) and caches the token in the token folder.
            GoogleDriveApi api = await GoogleDriveApi.CreateBuilder()
                .SetCredentialsPath(_credentialsPathBox.Text)
                .SetTokenFolderPath(_tokenFolderBox.Text)
                .SetApplicationName(_appNameBox.Text)
                .BuildAsync();

            Connected?.Invoke(this, api);
        }
        catch (Exception ex)
        {
            _errorLabel.Text = $"{ex.GetType().Name}: {ex.Message}";
            _connectButton.Enabled = true;
            _connectButton.Text = "Connect";
        }
    }
}
