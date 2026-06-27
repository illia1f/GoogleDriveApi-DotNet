# Folders and Hierarchy

How to create, list, rename, move, and delete folders — and what you need to know about Google
Drive's folder model (it is not a strict tree).

> These snippets assume you already have an authorized `gDriveApi`. See
> [Getting Started](../getting-started.md).

## Creating folders

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string newFolderId = await gDriveApi.Folders.CreateAsync(folderName: "NewFolderName"); // Add: cancellationToken: cts.Token

// Create a nested folder by passing a parent id
string childFolderId = await gDriveApi.Folders.CreateAsync(
    folderName: "NewFolderNameV2",
    parentFolderId: newFolderId); // Add: cancellationToken: cts.Token

Console.WriteLine("New Folder ID:  " + newFolderId);
Console.WriteLine("Child Folder ID: " + childFolderId);
```

## Listing folders

Retrieve folders in a parent and walk one level of children. `Folders.ListAsync` returns
[`DriveItem`](managing-files.md#the-driveitem-model) (`Id`, `Name`, `MimeType`):

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var folders = await gDriveApi.Folders.ListAsync(parentFolderId: "root"); // Add: cancellationToken: cts.Token

for (int i = 0; i < folders.Count; i++)
{
    var folder = folders[i];
    Console.WriteLine($"{i + 1}. [{folder.Name}] with ID({folder.Id})");

    var subFolders = await gDriveApi.Folders.ListAsync(folder.Id); // Add: cancellationToken: cts.Token
    for (int j = 0; j < subFolders.Count; j++)
    {
        var subFolder = subFolders[j];
        Console.WriteLine($"---|{j + 1}. [{subFolder.Name}] with ID({subFolder.Id})");
    }
}
```

Need more than id, name, and kind (size, modified time, and so on)? Pass a [`DriveFields`](managing-files.md#selecting-extra-fields-drivefields)
selector to get the raw `Google.Apis.Drive.v3.Data.File` carrying exactly those fields.

To retrieve the entire folder tree in one call, use `Folders.ListAllAsync`. Building a tree needs
each folder's parents, a variable field that is not on `DriveItem`, so use the field-selected
overload with `DriveFields.Default.WithParents()`, which returns the raw `File` carrying `parents`:

```csharp
var folders = await gDriveApi.Folders.ListAllAsync(DriveFields.Default.WithParents()); // → IReadOnlyList<File>
// each folder.Parents is now populated
```

See the [RetrieveAllFolderHierarchy sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/RetrieveAllFolderHierarchy).

## Finding a folder by name

```csharp
string? folderId = await gDriveApi.Folders.FindIdByNameAsync("My Folder", parentFolderId: "root");
if (folderId is null)
{
    Console.WriteLine("No matching folder found.");
}
```

## Deleting a folder

```csharp
await gDriveApi.Folders.DeleteAsync(folderId); // Add: cancellationToken
```

`Folders.DeleteAsync` validates that the id refers to a folder and throws
`InvalidMimeTypeException` if it does not. See [Exceptions](../reference/exceptions.md).

## Renaming a folder

Renaming lives on the `Folders` operation group:

```csharp
await gDriveApi.Folders.RenameAsync(folderId, "RenamedFolder"); // Add: cancellationToken
```

## Moving a folder

Move a folder to a different parent (parents are updated, same as files):

```csharp
await gDriveApi.Folders.MoveAsync(
    folderId,
    sourceFolderId: currentParentId,
    destinationFolderId: newParentId); // Add: cancellationToken
```

Both `Folders.RenameAsync` and `Folders.MoveAsync` validate that the id refers to a folder and
throw `InvalidMimeTypeException` if it does not. See [Exceptions](../reference/exceptions.md).

---

## Understanding Drive's folder model

Google Drive does **not** use a strict tree. Two facts matter when you traverse it:

### Multiple parents

In a typical file system each folder has exactly one parent. In Google Drive a folder (or
file) can have **multiple parents**, so the same item can appear in more than one location.
This supports shortcuts and shared folders.

### Cycle dependencies

Multiple parents make cycles possible: a folder can end up referenced within its own
hierarchy, creating an infinite loop if you traverse naively. If you build your own recursive
traversal of the folder tree, you must guard against this.

For traversal strategies (visited-set tracking, depth limits, DFS with cycle detection), see
[Folder hierarchy internals](../contributing/folder-hierarchy-internals.md). A working
implementation lives in the
[RetrieveAllFolderHierarchy sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/RetrieveAllFolderHierarchy).
