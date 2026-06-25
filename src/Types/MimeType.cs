namespace GoogleDriveApi_DotNet.Types;

/// <summary>
/// A Google Drive item's MIME type. Owns the knowledge of which value denotes a folder, so callers
/// ask <see cref="IsFolder"/> instead of comparing raw MIME strings. Instances are created through
/// <see cref="Create"/>, which validates the value, so every <see cref="MimeType"/> is well-formed.
/// </summary>
public sealed record MimeType
{
    /// <summary>
    /// The validated, non-empty raw MIME type string (for example <c>application/pdf</c>).
    /// </summary>
    public string Value { get; }

    private MimeType(string value) => Value = value;

    /// <summary>
    /// Creates a validated <see cref="MimeType"/> from a raw string.
    /// </summary>
    /// <param name="value">The raw MIME type string returned by Drive.</param>
    /// <returns>A validated <see cref="MimeType"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid MIME type.</exception>
    public static MimeType Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("MIME type must not be null or empty.", nameof(value));

        ReadOnlySpan<char> span = value;
        int slash = span.IndexOf('/');
        if (slash < 0                              // no separator
            || span.LastIndexOf('/') != slash      // more than one '/'
            || span[..slash].IsWhiteSpace()        // empty/blank type
            || span[(slash + 1)..].IsWhiteSpace()) // empty/blank subtype
        {
            throw new ArgumentException($"'{value}' is not a valid MIME type (expected 'type/subtype').", nameof(value));
        }

        return new MimeType(value);
    }

    /// <summary>
    /// Gets a value indicating whether this MIME type denotes a Google Drive folder.
    /// </summary>
    public bool IsFolder => Value == GDriveMimeTypes.Folder;
}
