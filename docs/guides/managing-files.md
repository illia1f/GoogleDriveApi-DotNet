# Managing Files

Rename, move, copy, and delete files. For uploading and downloading see
[Uploading files](uploading-files.md) and [Downloading files](downloading-files.md).

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).
> Every async method accepts an optional `CancellationToken`.

## Rename a file

```csharp
try
{
    await gDriveApi.RenameFileAsync(fileId, "NewFileName.pdf");
    Console.WriteLine("Renamed.");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid arguments: {ex.Message}");
}
```

## Move a file

Move a file from one folder to another (parents are updated):

```csharp
try
{
    await gDriveApi.MoveFileToAsync(fileId, sourceFolderId, destinationFolderId);
    Console.WriteLine("Moved.");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid arguments: {ex.Message}");
}
```

## Copy a file

Copy to another folder, optionally renaming the copy:

```csharp
try
{
    string copiedFileId = await gDriveApi.CopyFileToAsync(
        fileId, destinationFolderId, newName: "CopiedFile.pdf"); // newName null keeps original
    Console.WriteLine($"Copied with new ID: {copiedFileId}");
}
catch (GoogleApiException ex)
{
    Console.WriteLine($"Copy failed ({ex.HttpStatusCode}): {ex.Message}");
}
```

## Delete a file

Permanently delete a file. The id must refer to a file, not a folder:

```csharp
try
{
    await gDriveApi.DeleteFileAsync(fileId);
    Console.WriteLine("Deleted.");
}
catch (InvalidMimeTypeException ex)
{
    Console.WriteLine($"Cannot delete: {ex.Message}");
}
```

> To delete a folder, use `DeleteFolderAsync` — see [Folders and hierarchy](folders-and-hierarchy.md).
> For exception details, see [Exceptions](../reference/exceptions.md).

---

See the runnable
[FileManagement sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileManagement).
