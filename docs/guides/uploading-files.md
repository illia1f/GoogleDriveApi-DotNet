# Uploading Files

Upload files to Google Drive from a local path or from a `Stream`.

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).
> Every async method accepts an optional `CancellationToken`; pass
> `new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token` to set a timeout.

> **MIME types:** `KnownMimeTypes` is from the external
> [MimeMapping](https://www.nuget.org/packages/MimeMapping) package. You can also pass standard
> MIME strings directly (`"image/jpeg"`, `"application/pdf"`, `"text/plain"`). For Drive-specific
> types (folders, Google Docs), use this library's `MimeType` constants and helpers — see
> [MIME types reference](../reference/mime-types.md).

## Upload from a path or a stream

```csharp
using GoogleDriveApi_DotNet;
using MimeMapping;

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync();

string filePath = "Files/Escanor.jpg";

try
{
    // Upload using a file path (into the root folder).
    string fileId = await gDriveApi.Transfers.UploadAsync(filePath, KnownMimeTypes.Jpeg);
    Console.WriteLine($"Uploaded with ID({fileId})");

    // Upload using a Stream, directly into a target folder.
    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    string fileId2 = await gDriveApi.Transfers.UploadAsync(
        stream, fileName, KnownMimeTypes.Jpeg, parentFolderId: "your-folder-id");
    Console.WriteLine($"Uploaded with ID({fileId2})");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled or timed out.");
}
catch (UploadFileException ex)
{
    Console.WriteLine(ex.Message);
}
```

`Transfers.UploadAsync` and `Transfers.UploadAsync` both default to the root folder when
`parentFolderId` is omitted. On failure they throw `UploadFileException` (cause in
`InnerException`); see [Exceptions](../reference/exceptions.md).

## Updating existing file content

To replace the bytes of an existing file (metadata preserved):

```csharp
using var stream = File.OpenRead("Files/updated.pdf");
await gDriveApi.Transfers.UpdateContentAsync(fileId, stream, "application/pdf");
```

---

See the runnable
[FileUploader sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileUploader).
