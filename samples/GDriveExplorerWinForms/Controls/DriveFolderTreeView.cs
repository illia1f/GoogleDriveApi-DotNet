using GDriveExplorerWinForms.Services;

namespace GDriveExplorerWinForms.Controls;

/// <summary>
/// TreeView for Drive folders with lazy loading, shared by the explorer and the folder picker:
/// every unexpanded node carries a single placeholder child; expanding it loads the real
/// children via <see cref="DriveExplorerService.GetFoldersAsync"/>. Callers own the
/// BeforeExpand wiring so each can apply its own busy/error policy.
/// </summary>
public sealed class DriveFolderTreeView : TreeView
{
    private const string LazyPlaceholder = "…";

    private readonly DriveExplorerService _service;

    public DriveFolderTreeView(DriveExplorerService service)
    {
        _service = service;
        Font = Theme.BaseFont;
        ShowLines = false;
        FullRowSelect = true;
        HideSelection = false;
        ItemHeight = 26;
    }

    /// <summary>
    /// Adds the "My Drive" root node (with placeholder) and selects it.
    /// </summary>
    public void InitializeRoot()
    {
        var root = new TreeNode($"{Icons.Drive}  My Drive") { Name = "My Drive", Tag = _service.RootFolderId };
        root.Nodes.Add(LazyPlaceholder);
        Nodes.Add(root);
        SelectedNode = root;
    }

    /// <summary>
    /// True while the node's children have not been loaded yet.
    /// </summary>
    public static bool HasLazyPlaceholder(TreeNode node) =>
        node.Nodes.Count == 1 && node.Nodes[0].Text == LazyPlaceholder;

    /// <summary>
    /// Replaces the placeholder with the node's real child folders. The placeholder is only
    /// removed after the call succeeds, so a failed load can be retried by expanding again.
    /// </summary>
    public async Task LoadChildrenAsync(TreeNode node, CancellationToken ct)
    {
        IReadOnlyList<(string id, string name)> folders = await _service.GetFoldersAsync((string)node.Tag!, ct);
        node.Nodes.Clear();
        foreach ((string id, string name) in folders)
        {
            node.Nodes.Add(CreateFolderNode(id, name));
        }
    }

    /// <summary>
    /// Collapses the node and resets it to the unloaded (placeholder) state.
    /// </summary>
    public static void ResetNode(TreeNode node)
    {
        node.Collapse();
        node.Nodes.Clear();
        node.Nodes.Add(LazyPlaceholder);
    }

    // Node.Text carries an icon prefix for display; Node.Name keeps the raw folder name.
    private static TreeNode CreateFolderNode(string id, string name)
    {
        var node = new TreeNode($"{Icons.Folder}  {name}") { Name = name, Tag = id };
        node.Nodes.Add(LazyPlaceholder);
        return node;
    }
}
