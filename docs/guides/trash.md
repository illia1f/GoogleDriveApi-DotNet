# Trash

Soft-delete files (move to trash), restore them, list trashed items, and empty the trash.

> Snippets assume an authorized `gDriveApi` — see [Getting Started](../getting-started.md).
> Every async method accepts an optional `CancellationToken`.

## Move a file to trash

```csharp
try
{
    await gDriveApi.Trash.TrashAsync(fileId);
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
    await gDriveApi.Trash.RestoreAsync(fileId);
    Console.WriteLine("Restored.");
}
catch (GoogleApiException ex)
{
    Console.WriteLine($"Failed to restore ({ex.HttpStatusCode}): {ex.Message}");
}
```

## List trashed items

`Trash.ListAsync` returns [`DriveItem`](managing-files.md#the-driveitem-model) (`Id`, `Name`,
`MimeType`), paginated across all pages:

```csharp
var trashed = await gDriveApi.Trash.ListAsync();
Console.WriteLine($"{trashed.Count} item(s) in trash.");
foreach (var item in trashed)
{
    Console.WriteLine($"{item.Name} — {item.Id}");
}
```

For extra metadata (size, modified time, and so on) pass a
[`DriveFields`](managing-files.md#selecting-extra-fields-drivefields) selector to get the raw
`Google.Apis.Drive.v3.Data.File`:

```csharp
var trashed = await gDriveApi.Trash.ListAsync(DriveFields.Default.WithSize().WithModifiedTime());
```

## Empty the trash

> **Permanent.** Emptying the trash deletes everything in it irreversibly.

```csharp
await gDriveApi.Trash.EmptyAsync();
```

---

See the runnable
[TrashOperations sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/TrashOperations).
