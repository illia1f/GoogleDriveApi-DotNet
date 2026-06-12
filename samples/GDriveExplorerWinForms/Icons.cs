namespace GDriveExplorerWinForms;

/// <summary>
/// Centralized icon glyphs used in toolbars, menus, tree nodes and pane headers.
/// Constants hold the glyph only; call sites add the label and spacing.
/// </summary>
internal static class Icons
{
    // Entities
    public const string Drive = "☁";
    public const string Folder = "📁";
    public const string File = "📄";
    public const string Log = "📋";

    // Actions
    public const string Upload = "⬆";
    public const string Download = "⬇";
    public const string Rename = "✏";
    public const string Move = "➡";
    public const string Copy = "⧉";
    public const string UpdateContent = "⟳";
    public const string Trash = "🗑";
    public const string TrashBin = "♻";
    public const string Delete = "✖";
    public const string Refresh = "🔄";
    public const string Find = "🔍";
    public const string Cancel = "✕";

    // Status
    public const string StatusDot = "●";
}
