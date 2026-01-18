using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Exceptions;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

const string parentFolderId = "root";

// Change to your folder name in Google Drive
const string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = await gDriveApi.GetFolderIdByAsync(sourceFolderName, parentFolderId, cts.Token);
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name \"{sourceFolderName}\".");
    return;
}

/////////// Creating a copy of file ///////////

// Adjust this file name
string fileToCopyName = "TextFile";
string? fileId = await gDriveApi.GetFileIdByAsync(fileToCopyName, sourceFolderId, cts.Token);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name \"{fileToCopyName}\".");
    return;
}

string copyFileId;

try
{
    copyFileId = await gDriveApi.CopyFileToAsync(fileId, sourceFolderId, cancellationToken: cts.Token);
    Console.WriteLine($"Created a copy of file \"{fileToCopyName}\".");
}
catch (CopyFileException ex)
{
    Console.WriteLine($"Cannot create a copy of file \"{fileToCopyName}\". Details: {ex.Message}");
    return;
}

/////////// Renaming copy file ///////////

// Adjust this file name as well
const string newFileName = "MyCopyTextFile";

await gDriveApi.RenameFileAsync(copyFileId, newFileName, cancellationToken: cts.Token);

Console.WriteLine($"Renamed file \"{fileToCopyName}\" to \"{newFileName}\".");

/////////// Moving copy file ///////////

const string destinationFolderName = "1";
string? destinationFolderId = await gDriveApi.GetFolderIdByAsync(destinationFolderName, sourceFolderId, cts.Token);
if (destinationFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name \"{destinationFolderName}\".");
    return;
}

await gDriveApi.MoveFileToAsync(copyFileId, sourceFolderId, destinationFolderId, cts.Token);

Console.WriteLine($"File \"{newFileName}\" moved to folder \"{destinationFolderName}\".");

/////////// Update copy file with new content ///////////

// Add your file to samples/Shared folder and adjust below variables.
// Also change file name in current project .csproj file.
const string newContentFileName = "NewTextFileCopy.txt";
const string contentType = "text/plain";

if (!File.Exists(Path.Combine(AppContext.BaseDirectory, newContentFileName)))
{
    throw new FileNotFoundException("Content file not found.", newContentFileName);
}

using var stream = File.OpenRead(
    Path.Combine(AppContext.BaseDirectory, newContentFileName));

if (stream.Length == 0)
{
    throw new InvalidOperationException("Content file is empty.");
}

await gDriveApi.UpdateFileContentAsync(
    fileId: copyFileId,
    content: stream,
    contentType: contentType,
    cancellationToken: cts.Token);

Console.WriteLine($"File \"{newFileName}\" content updated.");