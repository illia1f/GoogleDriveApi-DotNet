using GDriveExplorerWinForms.Dialogs;
using GDriveExplorerWinForms.Services;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// Explorer-style main view: folder tree (lazy-loaded), file list, toolbar with every file
/// operation the library offers, search box, operation log panel and a status bar with
/// auth/token state. All Drive calls go through <see cref="DriveExplorerService"/> and run
/// one at a time via <see cref="OperationRunner"/>. UI construction lives in the
/// <c>ExplorerPanel.Ui.cs</c> partial; this file holds the state and the event handlers.
/// </summary>
public sealed partial class ExplorerPanel : UserControl
{
    private readonly DriveExplorerService _service;
    private readonly OperationRunner _runner = new();

    private readonly DriveFolderTreeView _folderTree;
    private readonly DriveFileListView _fileList;
    private readonly Label _filePaneHeader;
    private readonly OperationLogView _logView;
    private readonly ToolStrip _toolbar;
    private readonly ToolStripTextBox _searchBox;
    private readonly ToolStripButton _cancelButton;
    private readonly ExplorerStatusBar _statusBar;

    private bool _reloadPending;

    /// <summary>
    /// Toolbar items that act on the selected file — disabled while nothing is selected.
    /// </summary>
    private readonly List<ToolStripItem> _fileCommands = [];

    public ExplorerPanel(DriveExplorerService service)
    {
        _service = service;
        BackColor = Theme.Surface;

        _searchBox = CreateSearchBox();
        _cancelButton = CreateCancelButton();
        _toolbar = BuildToolbar();
        _folderTree = BuildFolderTree();
        _fileList = BuildFileList();
        _logView = new OperationLogView(service.Logger) { Dock = DockStyle.Fill };
        _filePaneHeader = MakePaneHeader($"{Icons.File}  FILES — My Drive");
        _statusBar = new ExplorerStatusBar(service, () => (CurrentFolderName, CurrentFolderId));
        _statusBar.TokenRefreshRequested += OnTokenRefreshRequested;
        _runner.BusyChanged += OnBusyChanged;

        BuildLayout();

        _folderTree.InitializeRoot();
        _statusBar.RefreshStatus();
    }

    private string CurrentFolderId => _folderTree.SelectedNode?.Tag as string ?? _service.RootFolderId;

    private string CurrentFolderName =>
        string.IsNullOrEmpty(_folderTree.SelectedNode?.Name) ? "My Drive" : _folderTree.SelectedNode!.Name;

    private GoogleFile? SelectedFile => _fileList.SelectedFile;

    /// <summary>
    /// Single rule for file commands: not busy and a file is selected.
    /// </summary>
    private bool CanActOnFile => !_runner.IsBusy && SelectedFile is not null;

    private async void OnBeforeExpandFolder(object? sender, TreeViewCancelEventArgs e)
    {
        TreeNode node = e.Node!;
        if (!DriveFolderTreeView.HasLazyPlaceholder(node))
        {
            return;
        }

        if (_runner.IsBusy)
        {
            // Another operation is running; keep the node collapsed so the user can retry,
            // instead of expanding into the unloaded placeholder.
            e.Cancel = true;
            return;
        }

        await RunOperationAsync(ct => _folderTree.LoadChildrenAsync(node, ct));
    }

    private async void OnFolderSelected(object? sender, TreeViewEventArgs e)
    {
        if (_runner.IsBusy)
        {
            // An operation is running; remember that the file pane is stale. RunOperationAsync
            // reloads when it finishes — CurrentFolderId is read then, so the latest selection wins.
            _reloadPending = true;
            return;
        }

        await ReloadFilesAsync();
    }

    private Task ReloadFilesAsync() =>
        RunOperationAsync(async ct =>
        {
            List<GoogleFile> files = await _service.GetFilesAsync(CurrentFolderId, ct);
            _fileList.ShowFiles(files);
            _filePaneHeader.Text = $"{Icons.File}  FILES — {CurrentFolderName}";
        });

    /// <summary>
    /// Re-loads the children of the currently selected tree node (after folder mutations).
    /// </summary>
    private void InvalidateCurrentTreeNode()
    {
        TreeNode? node = _folderTree.SelectedNode;
        if (node is null)
        {
            return;
        }

        DriveFolderTreeView.ResetNode(node);
    }

    private async void OnUpload(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Title = "Upload to Google Drive", Multiselect = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await UploadPathsAsync(dialog.FileNames, useStream: false);
    }

    private async void OnUploadFromStream(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Title = "Upload to Google Drive (stream variant)" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await UploadPathsAsync([dialog.FileName], useStream: true);
    }

    private Task UploadPathsAsync(IReadOnlyCollection<string> paths, bool useStream) =>
        RunOperationAsync(async ct =>
        {
            string targetFolderId = CurrentFolderId;
            foreach (string path in paths)
            {
                _ = useStream
                    ? await _service.UploadFileStreamAsync(path, targetFolderId, ct)
                    : await _service.UploadFileAsync(path, targetFolderId, ct);
            }
        }, reloadFilesAfter: true);

    private async void OnNewFolder(object? sender, EventArgs e)
    {
        string? name = PromptDialog.Show(this, "New folder", $"Folder name (created in \"{CurrentFolderName}\"):");
        if (name is null)
        {
            return;
        }

        string parentId = CurrentFolderId;
        await RunOperationAsync(ct => _service.CreateFolderAsync(name, parentId, ct));
        InvalidateCurrentTreeNode();
        _folderTree.SelectedNode?.Expand();
    }

    private async void OnDownload(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        using var dialog = new FolderBrowserDialog { Description = $"Download \"{file.Name}\" to…" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await RunOperationAsync(ct => _service.DownloadFileAsync(file.Id, dialog.SelectedPath, ct));
    }

    private async void OnRename(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        string? newName = PromptDialog.Show(this, "Rename file", "New name:", file.Name);
        if (newName is null || newName == file.Name)
        {
            return;
        }

        await RunOperationAsync(ct => _service.RenameFileAsync(file.Id, newName, ct), reloadFilesAfter: true);
    }

    private async void OnMove(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        (string id, string name)? destination = FolderPickerDialog.Show(this, _service, $"Move \"{file.Name}\" to…");
        if (destination is null)
        {
            return;
        }

        string sourceFolderId = CurrentFolderId;
        await RunOperationAsync(ct => _service.MoveFileAsync(file.Id, sourceFolderId, destination.Value.id, ct), reloadFilesAfter: true);
    }

    private async void OnCopy(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        (string id, string name)? destination = FolderPickerDialog.Show(this, _service, $"Copy \"{file.Name}\" to…");
        if (destination is null)
        {
            return;
        }

        string? copyName = PromptDialog.Show(this, "Copy file", "Name for the copy (leave as-is to keep):", file.Name);
        if (copyName is null)
        {
            return;
        }

        string? newName = copyName == file.Name ? null : copyName; // null = keep the original name
        await RunOperationAsync(ct => _service.CopyFileAsync(file.Id, destination.Value.id, newName, ct), reloadFilesAfter: true);
    }

    private async void OnUpdateContent(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        using var dialog = new OpenFileDialog { Title = $"Replace content of \"{file.Name}\" with…" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await RunOperationAsync(ct => _service.UpdateFileContentAsync(file.Id, dialog.FileName, ct), reloadFilesAfter: true);
    }

    private async void OnTrash(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        await RunOperationAsync(ct => _service.TrashFileAsync(file.Id, ct), reloadFilesAfter: true);
    }

    private async void OnDeleteFile(object? sender, EventArgs e)
    {
        if (SelectedFile is not { } file)
        {
            return;
        }

        if (Confirm($"Permanently delete file \"{file.Name}\"?\nThis cannot be undone."))
        {
            await RunOperationAsync(ct => _service.DeleteFileAsync(file.Id, ct), reloadFilesAfter: true);
        }
    }

    private async void OnDeleteFolder(object? sender, EventArgs e)
    {
        TreeNode? node = _folderTree.SelectedNode;
        if (node is null || node.Parent is null)
        {
            return; // never delete the root
        }

        if (Confirm($"Permanently delete folder \"{node.Name}\" and its contents?\nThis cannot be undone."))
        {
            string folderId = (string)node.Tag!;
            await RunOperationAsync(ct => _service.DeleteFolderAsync(folderId, ct));
            TreeNode parent = node.Parent;
            _folderTree.SelectedNode = parent;
            node.Remove();
        }
    }

    /// <summary>
    /// Re-loads the selected tree node's children and the file pane (tree context menu).
    /// </summary>
    private async void OnRefreshFolder(object? sender, EventArgs e)
    {
        // Reload the file pane first: Expand() below starts its own operation, and a reload
        // requested while it runs would only be queued via _reloadPending.
        await ReloadFilesAsync();
        InvalidateCurrentTreeNode();
        _folderTree.SelectedNode?.Expand();
    }

    private async void OnViewTrash(object? sender, EventArgs e)
    {
        using var dialog = new TrashDialog(_service);
        dialog.ShowDialog(this);
        await ReloadFilesAsync(); // restores may bring files back into view
    }

    private async void OnRefresh(object? sender, EventArgs e) => await ReloadFilesAsync();

    private async void OnFind(object? sender, EventArgs e)
    {
        string name = _searchBox.Text.Trim();
        if (name.Length == 0)
        {
            return;
        }

        string folderId = CurrentFolderId;
        await RunOperationAsync(async ct =>
        {
            string? fileId = await _service.FindFileIdAsync(name, folderId, ct);
            if (fileId is not null)
            {
                _fileList.SelectFile(fileId);
                return;
            }

            string? subFolderId = await _service.FindFolderIdAsync(name, folderId, ct);
            if (subFolderId is not null)
            {
                await SelectFolderInTreeAsync(subFolderId, ct);
            }
        });
    }

    private async Task SelectFolderInTreeAsync(string folderId, CancellationToken ct)
    {
        TreeNode? current = _folderTree.SelectedNode;
        if (current is null)
        {
            return;
        }

        // This runs inside an operation, so BeforeExpand's lazy load would be skipped —
        // load the children directly before expanding.
        if (DriveFolderTreeView.HasLazyPlaceholder(current))
        {
            await _folderTree.LoadChildrenAsync(current, ct);
        }

        current.Expand();
        foreach (TreeNode child in current.Nodes)
        {
            if (child.Tag as string == folderId)
            {
                _folderTree.SelectedNode = child;
                _folderTree.Focus();
                return;
            }
        }
    }

    private async void OnTokenRefreshRequested(object? sender, EventArgs e) =>
        await RunOperationAsync(ct => _service.TryRefreshTokenAsync(ct));

    /// <summary>
    /// Runs a single operation via the <see cref="OperationRunner"/>. With
    /// <paramref name="reloadFilesAfter"/> the file pane is reloaded in the same gesture once
    /// the operation succeeds — skipped on failure/cancel because nothing changed.
    /// </summary>
    private async Task RunOperationAsync(Func<CancellationToken, Task> operation, bool reloadFilesAfter = false)
    {
        if (_runner.IsBusy)
        {
            return;
        }

        bool succeeded = await _runner.RunAsync(operation);
        _statusBar.RefreshStatus();

        bool reload = _reloadPending || (reloadFilesAfter && succeeded);
        _reloadPending = false;
        if (reload)
        {
            await ReloadFilesAsync();
        }
    }

    private void OnBusyChanged(bool busy)
    {
        _statusBar.SetBusy(busy);
        UpdateCommandStates();
    }

    /// <summary>
    /// Single place deciding which commands are clickable: busy disables everything
    /// except Cancel; file commands additionally require a selected file.
    /// </summary>
    private void UpdateCommandStates()
    {
        bool busy = _runner.IsBusy;
        foreach (ToolStripItem item in _toolbar.Items)
        {
            item.Enabled = !busy;
        }

        foreach (ToolStripItem item in _fileCommands)
        {
            item.Enabled = CanActOnFile;
        }

        _cancelButton.Enabled = busy;
    }

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    // Shortcuts are routed here because ToolStripMenuItem.ShortcutKeys only works on a
    // form-level MenuStrip. Del/Shift+Del stay scoped to the file list so they don't
    // hijack text editing in the search box.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F5:
                OnRefresh(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.F:
                _searchBox.Focus();
                return true;
            case Keys.F2 when SelectedFile is not null:
                OnRename(this, EventArgs.Empty);
                return true;
            case Keys.Delete when _fileList.Focused && SelectedFile is not null:
                OnTrash(this, EventArgs.Empty);
                return true;
            case Keys.Shift | Keys.Delete when _fileList.Focused && SelectedFile is not null:
                OnDeleteFile(this, EventArgs.Empty);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
