using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// Details view of the current folder's files with alternating row colors. Accepts local
/// file drag &amp; drop and raises <see cref="FilesDropped"/> with the existing paths; the
/// host decides what activation (double-click / Enter) and drops mean.
/// </summary>
public sealed class DriveFileListView : ListView
{
    /// <summary>
    /// Raised when local files are dropped onto the list.
    /// </summary>
    public event Action<string[]>? FilesDropped;

    public DriveFileListView()
    {
        View = View.Details;
        BorderStyle = BorderStyle.None;
        Font = Theme.BaseFont;
        ForeColor = Theme.TextPrimary;
        FullRowSelect = true;
        MultiSelect = false;
        AllowDrop = true;
        HideSelection = false;

        Columns.Add("Name", 320);
        Columns.Add("Type", 220);
        Columns.Add("Size", 90, HorizontalAlignment.Right);
        Columns.Add("Modified", 150);

        DragEnter += OnDragEnterFiles;
        DragDrop += OnDragDropFiles;
    }

    public GoogleFile? SelectedFile => SelectedItems.Count > 0 ? (GoogleFile)SelectedItems[0].Tag! : null;

    /// <summary>
    /// Replaces the list contents with the given files, sorted by name.
    /// </summary>
    public void ShowFiles(IEnumerable<GoogleFile> files)
    {
        Items.Clear();
        int row = 0;
        foreach (GoogleFile file in files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = new ListViewItem(file.Name)
            {
                Tag = file,
                BackColor = row++ % 2 == 0 ? Theme.Surface : Theme.RowAlt,
            };
            item.SubItems.Add(file.MimeType);
            item.SubItems.Add(FormatSize(file.Size));
            item.SubItems.Add(file.ModifiedTimeDateTimeOffset?.LocalDateTime.ToString("g") ?? string.Empty);
            Items.Add(item);
        }
    }

    /// <summary>
    /// Selects and reveals the file with the given id, if present.
    /// </summary>
    public void SelectFile(string fileId)
    {
        foreach (ListViewItem item in Items)
        {
            if (((GoogleFile)item.Tag!).Id == fileId)
            {
                item.Selected = true;
                item.EnsureVisible();
                Focus();
                return;
            }
        }
    }

    private void OnDragEnterFiles(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnDragDropFiles(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            FilesDropped?.Invoke(paths.Where(File.Exists).ToArray());
        }
    }

    private static string FormatSize(long? bytes) => bytes switch
    {
        null => string.Empty,
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
    };
}
