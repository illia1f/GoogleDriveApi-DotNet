namespace GDriveExplorerWinForms.Dialogs;

/// <summary>
/// Minimal single-line text prompt used for "New folder", "Rename" and "Copy as" inputs.
/// </summary>
public sealed class PromptDialog : Form
{
    private readonly TextBox _inputBox;

    public string Value => _inputBox.Text.Trim();

    private PromptDialog(string title, string label, string initialValue)
    {
        Text = title;
        Font = Theme.BaseFont;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 136);

        var promptLabel = new Label
        {
            Text = label,
            ForeColor = Theme.TextSecondary,
            Font = Theme.SmallFont,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 6),
        };

        _inputBox = new TextBox
        {
            Text = initialValue,
            Dock = DockStyle.Top,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _inputBox.SelectAll();

        var okButton = new Button { Text = "OK", Size = new Size(80, 32), DialogResult = DialogResult.OK, Margin = new Padding(8, 10, 0, 0) };
        Theme.StylePrimaryButton(okButton);

        var cancelButton = new Button { Text = "Cancel", Size = new Size(80, 32), DialogResult = DialogResult.Cancel, Margin = new Padding(8, 10, 0, 0) };
        Theme.StyleSecondaryButton(cancelButton);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        buttons.Controls.Add(cancelButton); // first added = rightmost
        buttons.Controls.Add(okButton);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 14, 16, 12) };
        content.Controls.Add(_inputBox);
        content.Controls.Add(promptLabel);
        content.Controls.Add(buttons);
        Controls.Add(content);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    /// <summary>
    /// Shows the prompt; returns the trimmed value, or <c>null</c> when cancelled/empty.
    /// </summary>
    public static string? Show(IWin32Window owner, string title, string label, string initialValue = "")
    {
        using var dialog = new PromptDialog(title, label, initialValue);
        return dialog.ShowDialog(owner) == DialogResult.OK && dialog.Value.Length > 0 ? dialog.Value : null;
    }
}
