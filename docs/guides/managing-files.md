# Managing Files

Rename, move, copy, and delete files. For uploading and downloading see
[Uploading files](uploading-files.md) and [Downloading files](downloading-files.md).

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).
> Every async method accepts an optional `CancellationToken`.
>
> Rename, move, copy, and delete validate that the id refers to a file (not a folder) and throw
> `InvalidMimeTypeException` otherwise. See [Exceptions](../reference/exceptions.md).

## Listing files

`Files.ListAsync` returns the non-folder items in a folder as `DriveItem`, paginated across all pages:

```csharp
var files = await gDriveApi.Files.ListAsync(parentFolderId: "root"); // Add: cancellationToken
foreach (var file in files)
{
    Console.WriteLine($"{file.Name} — {file.Id}");
}
```

### The `DriveItem` model

`DriveItem` is the library's small read model. It carries only the core fields every Drive item has:

| Member     | Type       | Notes                                                                                |
| ---------- | ---------- | ------------------------------------------------------------------------------------ |
| `Id`       | `string`   | the item id                                                                          |
| `Name`     | `string`   | the item name                                                                        |
| `MimeType` | `MimeType` | value object; `MimeType.Value` is the raw string, `MimeType.IsFolder` the kind check |
| `IsFolder` | `bool`     | shortcut for `MimeType.IsFolder`                                                     |

Every field is always populated; a `DriveItem` never reports a field as "missing" because a
particular call did not fetch it. Fields that are optional or cost extra to fetch (size, modified
time, parents, owners, links) are deliberately not on the model. Reach them with `DriveFields`.

### Selecting extra fields (`DriveFields`)

When you need more than id, name, and kind, pass a `DriveFields` selector. The field-selected
overloads return the raw `Google.Apis.Drive.v3.Data.File` populated with exactly the fields you asked
for. Nothing is over-fetched, and an unset property is unambiguous: you simply did not request it.

```csharp
using GoogleDriveApi_DotNet.Types;

var files = await gDriveApi.Files.ListAsync(
    parentFolderId: "root",
    DriveFields.Default                  // always includes id, name, mimeType
        .WithSize()
        .WithModifiedTime()
        .WithWebViewLink()
        .WithRaw("owners,capabilities")); // escape hatch for fields without a dedicated With* method

foreach (var file in files) // file is Google.Apis.Drive.v3.Data.File
{
    Console.WriteLine($"{file.Name}  {file.Size}  {file.ModifiedTimeDateTimeOffset:g}");
}
```

To fetch a single item by id with selected fields, use `Files.FindByIdAsync`:

```csharp
var file = await gDriveApi.Files.FindByIdAsync(fileId, DriveFields.Default.WithSize());
if (file is null)
{
    // No item with that id — a 404 from Files.Get is mapped to null, not thrown.
    return;
}
```

The same `DriveFields` overload exists on `Folders.ListAsync`, `Folders.ListAllAsync`, and
`Trash.ListAsync`.

## Rename a file

```csharp
try
{
    await gDriveApi.Files.RenameAsync(fileId, "NewFileName.pdf");
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
    await gDriveApi.Files.MoveAsync(fileId, sourceFolderId, destinationFolderId);
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
    string copiedFileId = await gDriveApi.Files.CopyAsync(
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
    await gDriveApi.Files.DeleteAsync(fileId);
    Console.WriteLine("Deleted.");
}
catch (InvalidMimeTypeException ex)
{
    Console.WriteLine($"Cannot delete: {ex.Message}");
}
```

> To delete a folder, use `Folders.DeleteAsync` — see [Folders and hierarchy](folders-and-hierarchy.md).
> For exception details, see [Exceptions](../reference/exceptions.md).

---

See the runnable
[FileManagement sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileManagement).
