using GDriveExplorerWinForms.Services;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace GDriveExplorerWinForms.Dialogs;

/// <summary>
/// Trash manager: lists trashed items (<c>GetTrashedFilesAsync</c>) with Restore
/// (<c>RestoreFileFromTrashAsync</c>) and Empty Trash (<c>EmptyTrashAsync</c>) actions.
/// </summary>
public sealed class TrashDialog : Form
{
    private readonly DriveExplorerService _service;
    private readonly ListView _list;
    private readonly Button _restoreButton;
    private readonly Button _emptyButton;
    private readonly Button _refreshButton;

    public TrashDialog(DriveExplorerService service)
    {
        _service = service;

        Text = "Trash";
        Font = Theme.BaseFont;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 440);

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.BaseFont,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
        };
        _list.Columns.Add("Name", 300);
        _list.Columns.Add("Type", 220);
        _list.Columns.Add("Id", 80);

        _restoreButton = new Button { Text = "Restore", Size = new Size(110, 34), Margin = new Padding(0, 12, 8, 0) };
        Theme.StylePrimaryButton(_restoreButton);
        _restoreButton.Click += OnRestore;

        _emptyButton = new Button { Text = "Empty Trash", Size = new Size(110, 34), Margin = new Padding(0, 12, 8, 0) };
        Theme.StyleSecondaryButton(_emptyButton);
        _emptyButton.ForeColor = Theme.Error;
        _emptyButton.Click += OnEmptyTrash;

        _refreshButton = new Button { Text = "Refresh", Size = new Size(110, 34), Margin = new Padding(0, 12, 8, 0) };
        Theme.StyleSecondaryButton(_refreshButton);
        _refreshButton.Click += async (_, _) => await ReloadAsync();

        var closeButton = new Button
        {
            Text = "Close",
            Size = new Size(110, 34),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 12, 0, 0),
        };
        Theme.StyleSecondaryButton(closeButton);
        CancelButton = closeButton;

        // Action buttons on the left, Close pushed to the right by the percent-sized spacer column.
        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 5,
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.Controls.Add(_restoreButton, 0, 0);
        buttonRow.Controls.Add(_emptyButton, 1, 0);
        buttonRow.Controls.Add(_refreshButton, 2, 0);
        buttonRow.Controls.Add(closeButton, 4, 0);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        content.Controls.Add(_list);
        content.Controls.Add(buttonRow);
        Controls.Add(content);

        Shown += async (_, _) => await ReloadAsync();
    }

    private Task ReloadAsync() =>
        RunBusyAsync(async () =>
        {
            List<GoogleFile> items = await _service.GetTrashedFilesAsync(CancellationToken.None);
            _list.Items.Clear();
            foreach (GoogleFile item in items.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                var listItem = new ListViewItem(item.Name) { Tag = item };
                listItem.SubItems.Add(item.MimeType);
                listItem.SubItems.Add(item.Id);
                _list.Items.Add(listItem);
            }
        });

    private async void OnRestore(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        var file = (GoogleFile)_list.SelectedItems[0].Tag!;
        await RunBusyAsync(() => _service.RestoreFileAsync(file.Id, CancellationToken.None));
        await ReloadAsync();
    }

    private async void OnEmptyTrash(object? sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(this,
            "Permanently delete ALL items in trash?\nThis cannot be undone.",
            "Empty Trash", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        await RunBusyAsync(() => _service.EmptyTrashAsync(CancellationToken.None));
        await ReloadAsync();
    }

    /// <summary>
    /// Disables the action buttons and shows a wait cursor while the operation runs.
    /// Errors are swallowed because the service already logs them.
    /// </summary>
    private async Task RunBusyAsync(Func<Task> operation)
    {
        SetBusy(true);
        try
        {
            await operation();
        }
        catch
        {
            // already logged by the service
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _restoreButton.Enabled = !busy;
        _emptyButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        UseWaitCursor = busy;
    }
}
