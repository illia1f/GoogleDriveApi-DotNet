namespace GoogleDriveApi_DotNet.Types;

/// <summary>
/// A Google Drive item's MIME type. Owns the knowledge of which value denotes a folder and which
/// values are Google Workspace types, so callers ask <see cref="IsFolder"/> or
/// <see cref="IsGoogleWorkspace"/> instead of comparing raw MIME strings. Instances are created
/// through <see cref="Create"/>, which validates the value, so every <see cref="MimeType"/> is
/// well-formed.
/// </summary>
public sealed record MimeType
{
    /// <summary>
    /// The prefix shared by every Google Workspace MIME type.
    /// <para>Documentation: https://developers.google.com/drive/api/guides/mime-types?hl=en</para>
    /// </summary>
    public const string GoogleAppsPrefix = "application/vnd.google-apps";

    /// <summary>
    /// MIME type for Google Drive folders.
    /// </summary>
    public const string Folder = "application/vnd.google-apps.folder";

    /// <summary>
    /// MIME type for Google Docs.
    /// </summary>
    public const string Document = "application/vnd.google-apps.document";

    /// <summary>
    /// MIME type for Google Sheets.
    /// </summary>
    public const string Spreadsheet = "application/vnd.google-apps.spreadsheet";

    /// <summary>
    /// MIME type for Google Slides.
    /// </summary>
    public const string Presentation = "application/vnd.google-apps.presentation";

    /// <summary>
    /// MIME type for Google Drawings.
    /// </summary>
    public const string Drawing = "application/vnd.google-apps.drawing";

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
    public bool IsFolder => Value == Folder;

    /// <summary>
    /// Gets a value indicating whether this MIME type is a Google Workspace type (Docs, Sheets,
    /// Slides, Drawings, folders, and so on), identified by the <see cref="GoogleAppsPrefix"/>.
    /// Such types are not directly downloadable and must be exported to a standard format.
    /// </summary>
    public bool IsGoogleWorkspace =>
        Value.StartsWith(GoogleAppsPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Maps this Google Workspace MIME type to the exportable MIME type used to download it as a
    /// standard file format (for example Google Docs to <c>.docx</c>). Returns <c>null</c> when this
    /// type has no known export target.
    /// <para>Documentation: https://developers.google.com/drive/api/guides/ref-export-formats?hl=en</para>
    /// </summary>
    /// <returns>The exportable <see cref="MimeType"/>, or <c>null</c> when none is known.</returns>
    public MimeType? GetExportMimeType()
    {
        return Value switch
        {
            Document => new MimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            Spreadsheet => new MimeType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            Presentation => new MimeType("application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            Drawing => new MimeType("image/png"),
            _ => null,
        };
    }
}