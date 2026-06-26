using GDriveExplorerWinForms.Controls;
using GDriveExplorerWinForms.Services;

namespace GDriveExplorerWinForms.Dialogs;

/// <summary>
/// Destination picker for Move/Copy: a lazily-loaded folder tree backed by
/// <c>Folders.ListAsync</c>, mirroring the main explorer's tree behavior.
/// </summary>
public sealed class FolderPickerDialog : Form
{
    private readonly DriveFolderTreeView _tree;
    private readonly Button _okButton;

    private FolderPickerDialog(DriveExplorerService service, string title)
    {
        Text = title;
        Font = Theme.BaseFont;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 460);

        _tree = new DriveFolderTreeView(service)
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _tree.BeforeExpand += OnBeforeExpand;
        _tree.AfterSelect += (_, _) => _okButton!.Enabled = _tree.SelectedNode is not null;

        _okButton = new Button
        {
            Text = "Select",
            Size = new Size(80, 34),
            DialogResult = DialogResult.OK,
            Enabled = false,
            Margin = new Padding(8, 12, 0, 0),
        };
        Theme.StylePrimaryButton(_okButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Size = new Size(80, 34),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(8, 12, 0, 0),
        };
        Theme.StyleSecondaryButton(cancelButton);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        buttons.Controls.Add(cancelButton); // first added = rightmost
        buttons.Controls.Add(_okButton);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        content.Controls.Add(_tree);
        content.Controls.Add(buttons);
        Controls.Add(content);

        AcceptButton = _okButton;
        CancelButton = cancelButton;

        _tree.InitializeRoot();
    }

    private async void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        TreeNode node = e.Node!;
        if (!DriveFolderTreeView.HasLazyPlaceholder(node))
        {
            return;
        }

        try
        {
            await _tree.LoadChildrenAsync(node, CancellationToken.None);
        }
        catch
        {
            // already logged by the service; the placeholder stays for retry on next expand
        }
    }

    /// <summary>
    /// Shows the picker; returns the chosen folder's id and name, or <c>null</c> when cancelled.
    /// </summary>
    public static (string id, string name)? Show(IWin32Window owner, DriveExplorerService service, string title)
    {
        using var dialog = new FolderPickerDialog(service, title);
        if (dialog.ShowDialog(owner) != DialogResult.OK || dialog._tree.SelectedNode is not { } node)
        {
            return null;
        }

        return ((string)node.Tag!, node.Name);
    }
}
