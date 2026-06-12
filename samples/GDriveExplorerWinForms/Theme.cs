namespace GDriveExplorerWinForms;

/// <summary>
/// Centralized colors and fonts giving the sample a modern, flat look.
/// </summary>
internal static class Theme
{
    public static readonly Color Accent = Color.FromArgb(26, 115, 232);        // Google blue
    public static readonly Color AccentDark = Color.FromArgb(21, 87, 176);
    public static readonly Color Success = Color.FromArgb(30, 142, 62);        // Google green
    public static readonly Color Warning = Color.FromArgb(227, 116, 0);
    public static readonly Color Error = Color.FromArgb(197, 34, 31);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceAlt = Color.FromArgb(241, 243, 244);
    public static readonly Color Border = Color.FromArgb(218, 220, 224);
    public static readonly Color TextPrimary = Color.FromArgb(32, 33, 36);
    public static readonly Color TextSecondary = Color.FromArgb(95, 99, 104);
    public static readonly Color HeaderBack = Color.FromArgb(232, 240, 254);   // light Google blue
    public static readonly Color HeaderText = Color.FromArgb(21, 87, 176);
    public static readonly Color RowAlt = Color.FromArgb(247, 250, 255);

    public static readonly Font BaseFont = new("Segoe UI", 9.5f);
    public static readonly Font TitleFont = new("Segoe UI Semibold", 16f);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f);
    public static readonly Font HeaderFont = new("Segoe UI Semibold", 9f);
    public static readonly Font MonoFont = new("Cascadia Code", 8.75f);

    /// <summary>
    /// Styles a button as a filled, flat accent button with hover feedback.
    /// </summary>
    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.Font = BaseFont;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.MouseOverBackColor = AccentDark;
    }

    /// <summary>
    /// Styles a button as a bordered, flat secondary button.
    /// </summary>
    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Surface;
        button.ForeColor = TextPrimary;
        button.Font = BaseFont;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.MouseOverBackColor = SurfaceAlt;
    }
}
