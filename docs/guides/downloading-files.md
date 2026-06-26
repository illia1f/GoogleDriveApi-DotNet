# Downloading Files

Download files from Google Drive by id. The library handles both standard binary files and
Google Workspace files (Docs, Sheets, Slides, Drawings), which are **exported** to a compatible
format before download.

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).

## How download works

`Transfers.DownloadAsync(fileId, saveToPath)`:

1. Fetches the file metadata by id.
2. Determines whether the file is a Google Workspace (Google-specific) MIME type.
3. If it is, converts the MIME type to an exportable format; otherwise downloads the binary as-is.
4. Ensures the destination directory exists and writes the file there.

**Parameters:**

- `fileId` — the id of the file to download.
- `saveToPath` — the local **directory** to save into (default `"Downloads"`); created if missing.

## Why Workspace files are exported

Google Workspace files (Docs, Sheets, Slides, Drawings) are not stored as ordinary binaries, so
they cannot be downloaded directly. They must be **exported** to a standard format (e.g., a
Google Doc to `.docx` or `.pdf`) so the result can be opened and edited outside Google Workspace.
The library picks an export MIME type for these automatically.

## Example: find then download

```csharp
// Recommended for long-running downloads:
// using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

string parentFolderId = "root";
string? sourceFolderId = await gDriveApi.Folders.FindIdByNameAsync("FileDownloader Test Folder", parentFolderId);
if (sourceFolderId is null)
{
    Console.WriteLine("Source folder not found.");
    return;
}

string? fileId = await gDriveApi.Files.FindIdByNameAsync("Lesson_1.pdf", sourceFolderId);
if (fileId is null)
{
    Console.WriteLine("File not found.");
    return;
}

try
{
    await gDriveApi.Transfers.DownloadAsync(fileId); // saves into "Downloads" by default
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download was cancelled or timed out.");
}
```

On failure `Transfers.DownloadAsync` throws `DownloadFileException`; unsupported export types throw
`UnsupportedMimeTypeException`. See [Exceptions](../reference/exceptions.md).

---

See the runnable
[FileDownloader sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileDownloader).
