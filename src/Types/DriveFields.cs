using System.Collections.Immutable;

namespace GoogleDriveApi_DotNet.Types;

/// <summary>
/// An immutable, composable selector for the metadata fields a field-selected read should fetch.
/// <para>
/// Start from <see cref="Default"/> (which always carries the invariant core: <c>id</c>, <c>name</c>,
/// <c>mimeType</c>) and chain <c>With*</c> calls to add fields. Each call returns a new instance, so a
/// <see cref="DriveFields"/> value can be built up and shared safely.
/// </para>
/// <para>
/// Selecting only the fields you need avoids over-fetching, and — because the field-selected reads
/// return the raw <see cref="Google.Apis.Drive.v3.Data.File"/> — an unset property is unambiguous:
/// it was simply not requested.
/// </para>
/// </summary>
public sealed class DriveFields
{
    /// <summary>
    /// The invariant core every result carries, so an item always has identity and kind.
    /// </summary>
    private static readonly ImmutableArray<string> CoreFields = ["id", "name", "mimeType"];

    /// <summary>
    /// Known field tokens, in insertion order and deduplicated.
    /// </summary>
    private readonly ImmutableArray<string> _fields;

    /// <summary>
    /// Verbatim field chunks supplied through <see cref="WithRaw"/>, appended as-is so nested
    /// selector syntax (for example <c>owners(emailAddress)</c>) is preserved.
    /// </summary>
    private readonly ImmutableArray<string> _raw;

    private DriveFields(ImmutableArray<string> fields, ImmutableArray<string> raw)
    {
        _fields = fields;
        _raw = raw;
    }

    /// <summary>
    /// Gets the starting selector: the invariant core (<c>id</c>, <c>name</c>, <c>mimeType</c>) and
    /// nothing else.
    /// </summary>
    public static DriveFields Default { get; } = new(CoreFields, []);

    /// <summary>
    /// Returns a selector with <paramref name="field"/> appended, or the same instance when the field
    /// is already present.
    /// </summary>
    private DriveFields With(string field)
        => _fields.Contains(field) ? this : new DriveFields(_fields.Add(field), _raw);

    /// <summary>
    /// Adds the IDs of the item's parent folders (<c>parents</c>).
    /// </summary>
    public DriveFields WithParents() => With("parents");

    /// <summary>
    /// Adds the item's size in bytes (<c>size</c>); unset for folders and Google Workspace items.
    /// </summary>
    public DriveFields WithSize() => With("size");

    /// <summary>
    /// Adds the last-modified timestamp (<c>modifiedTime</c>).
    /// </summary>
    public DriveFields WithModifiedTime() => With("modifiedTime");

    /// <summary>
    /// Adds the creation timestamp (<c>createdTime</c>).
    /// </summary>
    public DriveFields WithCreatedTime() => With("createdTime");

    /// <summary>
    /// Adds the link for opening the item in a browser (<c>webViewLink</c>).
    /// </summary>
    public DriveFields WithWebViewLink() => With("webViewLink");

    /// <summary>
    /// Adds the link for downloading the item's content (<c>webContentLink</c>).
    /// </summary>
    public DriveFields WithWebContentLink() => With("webContentLink");

    /// <summary>
    /// Adds the MD5 checksum of the item's content (<c>md5Checksum</c>); binary files only.
    /// </summary>
    public DriveFields WithMd5Checksum() => With("md5Checksum");

    /// <summary>
    /// Adds the item's description (<c>description</c>).
    /// </summary>
    public DriveFields WithDescription() => With("description");

    /// <summary>
    /// Adds whether the item is starred (<c>starred</c>).
    /// </summary>
    public DriveFields WithStarred() => With("starred");

    /// <summary>
    /// Adds whether the item is trashed (<c>trashed</c>).
    /// </summary>
    public DriveFields WithTrashed() => With("trashed");

    /// <summary>
    /// Adds the item's icon link (<c>iconLink</c>).
    /// </summary>
    public DriveFields WithIconLink() => With("iconLink");

    /// <summary>
    /// Adds the item's thumbnail link (<c>thumbnailLink</c>).
    /// </summary>
    public DriveFields WithThumbnailLink() => With("thumbnailLink");

    /// <summary>
    /// Adds one or more fields verbatim — the escape hatch for fields without a dedicated <c>With*</c>
    /// method. The string is passed through unchanged, so nested selector syntax such as
    /// <c>owners(emailAddress,displayName)</c> is preserved.
    /// <para>
    /// The <em>structure</em> is checked (balanced parentheses, no empty or trailing tokens, no empty
    /// groups) so a typo surfaces here rather than as a deferred <c>400</c> from the API; the field
    /// <em>names</em> are not validated — Google is the authority on those.
    /// </para>
    /// <para>
    /// Raw fragments are appended verbatim and are <em>not</em> deduplicated against fields added via
    /// <c>With*</c> or earlier <see cref="WithRaw"/> calls, so avoid naming a field that is already selected.
    /// </para>
    /// </summary>
    /// <param name="fields">A Drive field selector fragment, for example <c>owners,capabilities,appProperties</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is <c>null</c>, empty, whitespace, or structurally malformed.</exception>
    public DriveFields WithRaw(string fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fields);
        ValidateFieldSyntax(fields);
        return new DriveFields(_fields, _raw.Add(fields));
    }

    /// <summary>
    /// Validates the bracket-and-comma structure of a raw field selector, without inspecting the field
    /// names themselves. Catches empty tokens, trailing commas, empty groups, and unbalanced
    /// parentheses; tolerates whitespace and nested groups.
    /// </summary>
    /// <param name="fields">The raw selector fragment to validate.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fields"/> is structurally malformed.</exception>
    private static void ValidateFieldSyntax(string fields)
    {
        int depth = 0;
        bool tokenHasContent = false;
        char previousDelimiter = '\0'; // last structural char seen: '(' ',' ')' — '\0' marks the start

        foreach (char c in fields)
        {
            switch (c)
            {
                case '(':
                    if (!tokenHasContent)
                        throw Malformed(fields, "'(' must follow a field name");
                    depth++;
                    previousDelimiter = '(';
                    tokenHasContent = false;
                    break;

                case ')':
                    if (depth == 0)
                        throw Malformed(fields, "unbalanced ')'");
                    // A field must precede ')', unless a nested group just closed (for example "a(b(c))").
                    if (!tokenHasContent && previousDelimiter != ')')
                        throw Malformed(fields, "empty field before ')'");
                    depth--;
                    previousDelimiter = ')';
                    tokenHasContent = false;
                    break;

                case ',':
                    // A field must precede ',', unless a group just closed (for example "a(b),c").
                    if (!tokenHasContent && previousDelimiter != ')')
                        throw Malformed(fields, "empty field around ','");
                    previousDelimiter = ',';
                    tokenHasContent = false;
                    break;

                default:
                    if (!char.IsWhiteSpace(c))
                        tokenHasContent = true;
                    break;
            }
        }

        if (depth != 0)
            throw Malformed(fields, "unbalanced '('");
        if (!tokenHasContent && previousDelimiter != ')')
            throw Malformed(fields, "empty trailing field");
    }

    private static ArgumentException Malformed(string fields, string reason)
        => new($"'{fields}' is not a valid field selector ({reason}).", nameof(fields));

    /// <summary>
    /// Renders the selector as a Drive <c>Files.Get</c> fields mask (a bare comma-separated list).
    /// </summary>
    internal string ToGetMask() => string.Join(",", _fields.Concat(_raw));

    /// <summary>
    /// Renders the selector as a Drive <c>Files.List</c> fields mask, wrapping the field list in
    /// <c>files(...)</c> and requesting <c>nextPageToken</c> for pagination.
    /// </summary>
    internal string ToListMask() => $"nextPageToken, files({ToGetMask()})";
}
