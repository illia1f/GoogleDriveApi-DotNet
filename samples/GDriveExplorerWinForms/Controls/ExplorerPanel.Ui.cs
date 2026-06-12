namespace GDriveExplorerWinForms.Controls;

// UI construction for ExplorerPanel: toolbar, panes, layout and context menus.
// State and event handlers live in ExplorerPanel.cs.
public sealed partial class ExplorerPanel
{
    private ToolStrip BuildToolbar()
    {
        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Theme.SurfaceAlt,
            Font = Theme.BaseFont,
            Padding = new Padding(6, 4, 6, 4),
            RenderMode = ToolStripRenderMode.System,
        };

        toolbar.Items.Add(MakeGroupCaption("FOLDER"));

        var uploadButton = new ToolStripSplitButton($"{Icons.Upload} Upload")
        {
            ToolTipText = "UploadFilePathAsync — upload local files to the current folder",
            ForeColor = Theme.TextPrimary,
        };
        uploadButton.ButtonClick += OnUpload;
        uploadButton.DropDownItems.Add(new ToolStripMenuItem("Upload via stream…", null, OnUploadFromStream)
        {
            ToolTipText = "UploadFileStreamAsync — upload from an open FileStream",
        });
        toolbar.Items.Add(uploadButton);
        AddToolButton(toolbar, $"{Icons.Folder} New Folder", "CreateFolderAsync — create a folder under the current one", OnNewFolder);
        toolbar.Items.Add(new ToolStripSeparator());

        toolbar.Items.Add(MakeGroupCaption("FILE"));
        _fileCommands.Add(AddToolButton(toolbar, $"{Icons.Download} Download", "DownloadFileAsync — download the selected file (Google Docs are exported automatically)", OnDownload));

        var organizeButton = new ToolStripDropDownButton("Organize")
        {
            ToolTipText = "Rename / move / copy / replace content / delete the selected file",
            ForeColor = Theme.TextPrimary,
        };
        organizeButton.DropDownItems.Add(new ToolStripMenuItem($"{Icons.Rename} Rename…", null, OnRename)
        {
            ShortcutKeyDisplayString = "F2",
            ToolTipText = "RenameFileAsync",
        });
        organizeButton.DropDownItems.Add(new ToolStripMenuItem($"{Icons.Move} Move…", null, OnMove) { ToolTipText = "MoveFileToAsync" });
        organizeButton.DropDownItems.Add(new ToolStripMenuItem($"{Icons.Copy} Copy…", null, OnCopy) { ToolTipText = "CopyFileToAsync" });
        organizeButton.DropDownItems.Add(new ToolStripMenuItem($"{Icons.UpdateContent} Update content…", null, OnUpdateContent) { ToolTipText = "UpdateFileContentAsync" });
        organizeButton.DropDownItems.Add(new ToolStripSeparator());
        organizeButton.DropDownItems.Add(new ToolStripMenuItem($"{Icons.Delete} Delete permanently…", null, OnDeleteFile)
        {
            ShortcutKeyDisplayString = "Shift+Del",
            ToolTipText = "DeleteFileAsync",
        });
        toolbar.Items.Add(organizeButton);
        _fileCommands.Add(organizeButton);

        _fileCommands.Add(AddToolButton(toolbar, $"{Icons.Trash} Trash", "MoveFileToTrashAsync — move the selected file to trash (Del)", OnTrash));
        toolbar.Items.Add(new ToolStripSeparator());

        toolbar.Items.Add(MakeGroupCaption("DRIVE"));
        AddToolButton(toolbar, $"{Icons.TrashBin} Trash Bin", "GetTrashedFilesAsync — open the trash manager", OnViewTrash);
        AddToolButton(toolbar, $"{Icons.Refresh} Refresh", "Reload the current folder (F5)", OnRefresh);
        toolbar.Items.Add(new ToolStripSeparator());

        toolbar.Items.Add(_searchBox);
        AddToolButton(toolbar, $"{Icons.Find} Find", "GetFileIdByAsync / GetFolderIdByAsync — find by exact name in the current folder", OnFind);

        toolbar.Items.Add(_cancelButton);
        return toolbar;
    }

    private ToolStripTextBox CreateSearchBox()
    {
        var box = new ToolStripTextBox { ToolTipText = "Name to find in the current folder (Ctrl+F)", AutoSize = false, Width = 160 };
        box.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OnFind(this, EventArgs.Empty); } };
        return box;
    }

    private ToolStripButton CreateCancelButton()
    {
        var button = new ToolStripButton($"{Icons.Cancel} Cancel")
        {
            Alignment = ToolStripItemAlignment.Right,
            ToolTipText = "Cancel the running operation (CancellationToken)",
            Enabled = false,
            ForeColor = Theme.Error,
        };
        button.Click += (_, _) => _runner.Cancel();
        return button;
    }

    private DriveFolderTreeView BuildFolderTree()
    {
        var tree = new DriveFolderTreeView(_service)
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary,
        };
        tree.BeforeExpand += OnBeforeExpandFolder;
        tree.AfterSelect += OnFolderSelected;
        // TreeView does not select on right-click by itself; without this the context menu
        // would act on the previously selected folder.
        tree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                tree.SelectedNode = e.Node;
            }
        };
        tree.ContextMenuStrip = BuildTreeContextMenu();
        return tree;
    }

    private DriveFileListView BuildFileList()
    {
        var list = new DriveFileListView { Dock = DockStyle.Fill };
        list.FilesDropped += async paths => await UploadPathsAsync(paths, useStream: false);
        list.ContextMenuStrip = BuildContextMenu();
        list.SelectedIndexChanged += (_, _) => UpdateCommandStates();
        list.ItemActivate += OnDownload; // double-click / Enter = download
        return list;
    }

    private void BuildLayout()
    {
        var innerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = Theme.Border,
        };
        innerSplit.Panel1.Controls.Add(_folderTree);
        innerSplit.Panel1.Controls.Add(MakePaneHeader($"{Icons.Folder}  FOLDERS"));
        innerSplit.Panel1.BackColor = Theme.SurfaceAlt;
        innerSplit.Panel2.Controls.Add(_fileList);
        innerSplit.Panel2.Controls.Add(_filePaneHeader);
        innerSplit.Panel2.BackColor = Theme.Surface;

        var outerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel2,
            BackColor = Theme.Border,
        };
        outerSplit.Panel1.Controls.Add(innerSplit);
        outerSplit.Panel2.Controls.Add(_logView);
        outerSplit.Panel2.Controls.Add(MakePaneHeader($"{Icons.Log}  OPERATION LOG — every library call with arguments and timing"));

        Controls.Add(outerSplit);
        Controls.Add(_toolbar);
        Controls.Add(_statusBar);
        _toolbar.Dock = DockStyle.Top;
        _statusBar.Dock = DockStyle.Bottom;

        // HandleCreated fires before docking gives the control its real size, so set the
        // initial proportions on the first SizeChanged that reports a usable size.
        bool splittersInitialized = false;
        outerSplit.SizeChanged += (_, _) =>
        {
            if (splittersInitialized || outerSplit.Height < 300 || outerSplit.Width < 300)
            {
                return;
            }

            splittersInitialized = true;
            outerSplit.SplitterDistance = (int)(outerSplit.Height * 0.65); // log pane gets ~35%
            innerSplit.SplitterDistance = (int)(innerSplit.Width * 0.4);   // folders 40% / files 60%
        };
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip { Font = Theme.BaseFont };
        List<ToolStripItem> fileItems =
        [
            menu.Items.Add($"{Icons.Download} Download", null, OnDownload),
            menu.Items.Add($"{Icons.Rename} Rename…", null, OnRename),
            menu.Items.Add($"{Icons.Move} Move…", null, OnMove),
            menu.Items.Add($"{Icons.Copy} Copy…", null, OnCopy),
            menu.Items.Add($"{Icons.UpdateContent} Update content…", null, OnUpdateContent),
        ];
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add($"{Icons.Upload} Upload here…", null, OnUpload);
        menu.Items.Add($"{Icons.Upload} Upload here (stream variant)…", null, OnUploadFromStream);
        menu.Items.Add(new ToolStripSeparator());
        fileItems.Add(menu.Items.Add($"{Icons.Trash} Move to trash", null, OnTrash));
        fileItems.Add(menu.Items.Add($"{Icons.Delete} Delete permanently…", null, OnDeleteFile));

        menu.Opening += (_, _) =>
        {
            foreach (ToolStripItem item in fileItems)
            {
                item.Enabled = CanActOnFile;
            }
        };
        return menu;
    }

    private ContextMenuStrip BuildTreeContextMenu()
    {
        var menu = new ContextMenuStrip { Font = Theme.BaseFont };
        menu.Items.Add($"{Icons.Folder} New folder…", null, OnNewFolder);
        menu.Items.Add($"{Icons.Refresh} Refresh", null, OnRefreshFolder);
        menu.Items.Add(new ToolStripSeparator());
        ToolStripItem deleteItem = menu.Items.Add($"{Icons.Delete} Delete folder…", null, OnDeleteFolder);

        menu.Opening += (_, _) => deleteItem.Enabled = !_runner.IsBusy && _folderTree.SelectedNode?.Parent is not null;
        return menu;
    }

    private static Label MakePaneHeader(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 30,
        BackColor = Theme.HeaderBack,
        ForeColor = Theme.HeaderText,
        Font = Theme.HeaderFont,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(8, 0, 0, 0),
    };

    private static ToolStripButton AddToolButton(ToolStrip toolbar, string text, string toolTip, EventHandler onClick)
    {
        var button = new ToolStripButton(text) { ToolTipText = toolTip, ForeColor = Theme.TextPrimary };
        button.Click += onClick;
        toolbar.Items.Add(button);
        return button;
    }

    private static ToolStripLabel MakeGroupCaption(string text) => new(text)
    {
        ForeColor = Theme.TextSecondary,
        Font = Theme.SmallFont,
        Margin = new Padding(6, 0, 2, 0),
    };
}
