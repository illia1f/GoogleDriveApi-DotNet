# Sample Code Snippets for GoogleDriveApi-DotNet

> **Note:** All async methods support an optional `CancellationToken` parameter for cancellation and timeout control. To set a timeout, use `new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token`. If not provided, operations will not timeout.

## Creating an Instance of GoogleDriveApi

Create an instance of the `GoogleDriveApi` class using the fluent builder pattern with your credentials and token paths.

```csharp
// Optional: Create a CancellationTokenSource for timeout control
// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(immediateAuthorization: false); // Add: cancellationToken: cts.Token

// If immediateAuthorization is false, it is necessary to invoke the Authorize method.
// Default value is true.
await gDriveApi.AuthorizeAsync(); // Add: cts.Token for timeout control
```

## Uploading files

Upload files to Google Drive using either a file path or a stream.

> **Note:** `KnownMimeTypes` is from the external [MimeMapping](https://www.nuget.org/packages/MimeMapping) NuGet package. You can also use standard MIME type strings directly (e.g., `"image/jpeg"`, `"application/pdf"`, `"text/plain"`). For Google Drive-specific types like folders or Google Docs, use the `GDriveMimeTypes` class from this library.

```csharp
using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(); // Add: cancellationToken: cts.Token

string filePath = "Files/Escanor.jpg";

try
{
    // Uploads a file to Google Drive using a file path.
    string fileId = await gDriveApi.UploadFilePathAsync(filePath, KnownMimeTypes.Jpeg); // Add: cts.Token

    Console.WriteLine($"File has been successfully uploaded with ID({fileId})");

    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    // Uploads a file to Google Drive using a Stream.
    string fileId2 = await gDriveApi.UploadFileStreamAsync(stream, fileName, KnownMimeTypes.Jpeg); // Add: cts.Token

    Console.WriteLine($"File has been successfully uploaded with ID({fileId2})");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled or timed out.");
}
catch (UploadFileException ex)
{
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
```

## Creating Folders

Create folders in Google Drive. The `CancellationToken` parameter is optional for all operations.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string newFolderId = await gDriveApi.CreateFolderAsync(folderName: "NewFolderName"); // Add: cancellationToken: cts.Token

string newFolderId2 = await gDriveApi.CreateFolderAsync(folderName: "NewFolderNameV2", parentFolderId: newFolderId); // Add: cancellationToken: cts.Token

Console.WriteLine("New Folder ID: " + newFolderId);
Console.WriteLine("New Folder ID2: " + newFolderId2);
```

## Retrieving a List of Folders

Retrieve and print all folders in the root directory and their children.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// Retrieves a list of folders in the root directory
var folders = await gDriveApi.GetFoldersByAsync(parentFolderId: "root"); // Add: cancellationToken: cts.Token

for (int i = 0; i < folders.Count; i++)
{
   var folder = folders[i];

   Console.WriteLine($"{i + 1}. [{folder.name}] with ID({folder.id})");

   // Retrieves a list of subfolders within the current folder
   var subFolders = await gDriveApi.GetFoldersByAsync(folder.id); // Add: cancellationToken: cts.Token
   for (int j = 0; j < subFolders.Count; j++)
   {
      var subFolder = subFolders[j];

      Console.WriteLine($"---|{j + 1}. [{subFolder.name}] with ID({subFolder.id})");
   }
}
```

## Downloading Files from Google Drive

Download files from Google Drive. For large files, consider using a `CancellationToken` with an appropriate timeout.

```csharp
// Recommended for long-running operations: using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = await gDriveApi.GetFolderIdByAsync(sourceFolderName, parentFolderId); // Add: cts.Token
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

string fullFileNameToDownload = "Lesson_1.pdf";

string? fileId = await gDriveApi.GetFileIdByAsync(fullFileNameToDownload, sourceFolderId); // Add: cts.Token
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fullFileNameToDownload}.");
    return;
}

try
{
    await gDriveApi.DownloadFileAsync(fileId); // Add: cancellationToken: cts.Token
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download was cancelled or timed out.");
}
```

## File Management Operations

### Renaming a File

Rename a file by updating its metadata.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";
string newName = "NewFileName.pdf";

try
{
    await gDriveApi.RenameFileAsync(fileId, newName); // Add: cancellationToken: cts.Token
    Console.WriteLine($"File renamed successfully to '{newName}'.");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid arguments: {ex.Message}");
}
```

### Moving a File

Move a file from one folder to another.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";
string sourceFolderId = "source-folder-id";
string destinationFolderId = "destination-folder-id";

try
{
    await gDriveApi.MoveFileToAsync(fileId, sourceFolderId, destinationFolderId); // Add: cancellationToken: cts.Token
    Console.WriteLine("File moved successfully.");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid arguments: {ex.Message}");
}
```

### Copying a File

Copy a file to a different folder with an optional new name.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";
string destinationFolderId = "destination-folder-id";
string? newName = "CopiedFile.pdf"; // Optional, null to keep original name

try
{
    string copiedFileId = await gDriveApi.CopyFileToAsync(fileId, destinationFolderId, newName); // Add: cancellationToken: cts.Token
    Console.WriteLine($"File copied successfully with new ID: {copiedFileId}");
}
catch (CopyFileException ex)
{
    Console.WriteLine($"Copy failed: {ex.Message}");
}
```

### Deleting a File

Permanently delete a file from Google Drive.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";

try
{
    await gDriveApi.DeleteFileAsync(fileId); // Add: cancellationToken: cts.Token
    Console.WriteLine("File deleted successfully.");
}
catch (InvalidFileTypeException ex)
{
    Console.WriteLine($"Cannot delete: {ex.Message}");
}
```

## Trash Operations

### Moving a File to Trash

Move a file to the Google Drive trash (soft delete).

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";

try
{
    await gDriveApi.MoveFileToTrashAsync(fileId); // Add: cancellationToken: cts.Token
    Console.WriteLine("File moved to trash successfully.");
}
catch (TrashFileException ex)
{
    Console.WriteLine($"Failed to trash file: {ex.Message}");
}
```

### Restoring a File from Trash

Restore a file from the Google Drive trash.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

string fileId = "your-file-id";

try
{
    await gDriveApi.RestoreFileFromTrashAsync(fileId); // Add: cancellationToken: cts.Token
    Console.WriteLine("File restored from trash successfully.");
}
catch (RestoreFileException ex)
{
    Console.WriteLine($"Failed to restore file: {ex.Message}");
}
```
