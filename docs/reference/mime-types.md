# MIME Types Reference

The `GDriveMimeTypes` static class (namespace `GoogleDriveApi_DotNet.Types`) holds the
Google-specific MIME type constants and helpers used across the library.

> Official: [Google Workspace MIME types](https://developers.google.com/drive/api/guides/mime-types) ·
> [Export formats](https://developers.google.com/drive/api/guides/ref-export-formats)

## Constants

- **`GoogleAppsPrefix`** = `application/vnd.google-apps` — prefix shared by all Google Workspace types
- **`Folder`** = `application/vnd.google-apps.folder` — a Drive folder
- **`Document`** = `application/vnd.google-apps.document` — Google Docs
- **`Spreadsheet`** = `application/vnd.google-apps.spreadsheet` — Google Sheets
- **`Presentation`** = `application/vnd.google-apps.presentation` — Google Slides
- **`Drawing`** = `application/vnd.google-apps.drawing` — Google Drawings

## Helpers

### `IsValid(string mimeType)`

Returns `true` when the MIME type is a Google Workspace / Drive type (starts with
`GoogleAppsPrefix`). Used to decide whether a file must be **exported** rather than downloaded
as a binary.

```csharp
bool isWorkspaceType = GDriveMimeTypes.IsValid(file.MimeType);
```

### `GetExportMimeTypeBy(string mimeType)`

Maps a Google Workspace type to the format it is exported as on download. Returns `null` for
unrecognized types:

- **`Document`** (Docs) → `application/vnd.openxmlformats-officedocument.wordprocessingml.document` (`.docx`)
- **`Spreadsheet`** (Sheets) → `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (`.xlsx`)
- **`Presentation`** (Slides) → `application/vnd.openxmlformats-officedocument.presentationml.presentation` (`.pptx`)
- **`Drawing`** (Drawings) → `image/png`
- anything else → `null`

This is why downloads of Workspace files can throw `UnsupportedMimeTypeException` — a type with
no export mapping cannot be saved. See [Downloading files](../guides/downloading-files.md) and
[Exceptions](exceptions.md).

## Non-Google types

For ordinary files, pass a standard MIME string (`"image/jpeg"`, `"application/pdf"`,
`"text/plain"`), or use the [MimeMapping](https://www.nuget.org/packages/MimeMapping) package's
`KnownMimeTypes` to look one up from a file name.
