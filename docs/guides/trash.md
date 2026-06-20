# Trash

Soft-delete files (move to trash), restore them, list trashed items, and empty the trash.

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).
> Every async method accepts an optional `CancellationToken`.

## Move a file to trash

```csharp
try
{
    await gDriveApi.MoveFileToTrashAsync(fileId);
    Console.WriteLine("Moved to trash.");
}
catch (GoogleApiException ex)
{
    Console.WriteLine($"Failed to trash ({ex.HttpStatusCode}): {ex.Message}");
}
```

## Restore a file from trash

```csharp
try
{
    await gDriveApi.RestoreFileFromTrashAsync(fileId);
    Console.WriteLine("Restored.");
}
catch (GoogleApiException ex)
{
    Console.WriteLine($"Failed to restore ({ex.HttpStatusCode}): {ex.Message}");
}
```

## List trashed items

```csharp
var trashed = await gDriveApi.GetTrashedFilesAsync(); // paginates all trashed items
Console.WriteLine($"{trashed.Count} item(s) in trash.");
```

## Empty the trash

> **Permanent.** Emptying the trash deletes everything in it irreversibly.

```csharp
await gDriveApi.EmptyTrashAsync();
```

---

See the runnable
[TrashOperations sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/TrashOperations).
