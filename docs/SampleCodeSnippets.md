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
gDriveApi.Authorize(); // Add: cts.Token for timeout control
```

## Uploading files

Upload files to Google Drive using either a file path or a stream.

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
    string fileId = gDriveApi.UploadFilePath(filePath, KnownMimeTypes.Jpeg); // Add: cts.Token

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");

    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    // Uploads a file to Google Drive using a Stream.
    gDriveApi.UploadFileStream(stream, fileName, KnownMimeTypes.Jpeg); // Add: cts.Token

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled or timed out.");
}
catch (CreateMediaUploadException ex)
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

string newFolderId = gDriveApi.CreateFolder(folderName: "NewFolderName"); // Add: cancellationToken: cts.Token

string newFolderId2 = gDriveApi.CreateFolder(folderName: "NewFolderNameV2", parentFolderId: newFolderId); // Add: cancellationToken: cts.Token

Console.WriteLine("New Folder ID: " + newFolderId);
Console.WriteLine("New Folder ID2: " + newFolderId2);
```

## Retrieving a List of Folders

Retrieve and print all folders in the root directory and their children.

```csharp
// Optional: using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// Retrieves a list of folders in the root directory
var folders = gDriveApi.GetFoldersBy(parentFolderId: "root"); // Add: cancellationToken: cts.Token

for (int i = 0; i < folders.Count; i++)
{
   var folder = folders[i];

   Console.WriteLine($"{i + 1}. [{folder.name}] with ID({folder.id})");

   // Retrieves a list of subfolders within the current folder
   var subFolders = gDriveApi.GetFoldersBy(folder.id); // Add: cancellationToken: cts.Token
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
string? sourceFolderId = gDriveApi.GetFolderIdBy(sourceFolderName, parentFolderId); // Add: cts.Token
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

string fullFileNameToDownload = "Lesson_1.pdf";

string? fileId = gDriveApi.GetFileIdBy(fullFileNameToDownload, sourceFolderId); // Add: cts.Token
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
