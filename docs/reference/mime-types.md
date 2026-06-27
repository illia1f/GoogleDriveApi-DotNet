# MIME Types Reference

The `MimeType` value object (namespace `GoogleDriveApi_DotNet.Types`) holds the Google-specific MIME
type constants and the kind/export helpers used across the library. It also validates its value, so
every `MimeType` is well-formed (`type/subtype`).

> Official: [Google Workspace MIME types](https://developers.google.com/drive/api/guides/mime-types) ·
> [Export formats](https://developers.google.com/drive/api/guides/ref-export-formats)

## Constants

- **`MimeType.GoogleAppsPrefix`** = `application/vnd.google-apps` — prefix shared by all Google Workspace types
- **`MimeType.Folder`** = `application/vnd.google-apps.folder` — a Drive folder
- **`MimeType.Document`** = `application/vnd.google-apps.document` — Google Docs
- **`MimeType.Spreadsheet`** = `application/vnd.google-apps.spreadsheet` — Google Sheets
- **`MimeType.Presentation`** = `application/vnd.google-apps.presentation` — Google Slides
- **`MimeType.Drawing`** = `application/vnd.google-apps.drawing` — Google Drawings

## Creating a `MimeType`

`MimeType.Create(string?)` validates the raw string and returns a `MimeType`. The raw value stays
reachable via `.Value`:

```csharp
MimeType mimeType = MimeType.Create(file.MimeType);
string raw = mimeType.Value; // e.g. "application/pdf"
```

It throws `ArgumentException` when the value is null, empty, or not a `type/subtype` string.

## Helpers

### `IsFolder`

Instance property. `true` when the type denotes a Drive folder (equals `MimeType.Folder`):

```csharp
bool isFolder = MimeType.Create(file.MimeType).IsFolder;
```

### `IsGoogleWorkspace`

Instance property. `true` when the type is a Google Workspace / Drive type (starts with
`GoogleAppsPrefix`). Used to decide whether a file must be **exported** rather than downloaded as a
binary:

```csharp
bool isWorkspaceType = MimeType.Create(file.MimeType).IsGoogleWorkspace;
```

### `GetExportMimeType()`

Instance method. Maps a Google Workspace type to the `MimeType` it is exported as on download.
Returns `null` for unrecognized types:

- **`Document`** (Docs) → `application/vnd.openxmlformats-officedocument.wordprocessingml.document` (`.docx`)
- **`Spreadsheet`** (Sheets) → `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (`.xlsx`)
- **`Presentation`** (Slides) → `application/vnd.openxmlformats-officedocument.presentationml.presentation` (`.pptx`)
- **`Drawing`** (Drawings) → `image/png`
- anything else → `null`

```csharp
MimeType? export = MimeType.Create(file.MimeType).GetExportMimeType();
```

This is why downloads of Workspace files can throw `UnsupportedMimeTypeException` — a type with
no export mapping cannot be saved. See [Downloading files](../guides/downloading-files.md) and
[Exceptions](exceptions.md).

## Non-Google types

For ordinary files, pass a standard MIME string (`"image/jpeg"`, `"application/pdf"`,
`"text/plain"`), or use the [MimeMapping](https://www.nuget.org/packages/MimeMapping) package's
`KnownMimeTypes` to look one up from a file name.
