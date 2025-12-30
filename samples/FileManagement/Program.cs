using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Exceptions;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = await gDriveApi.GetFolderIdByAsync(sourceFolderName, parentFolderId, cts.Token);
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

/////////// Creating a copy of file ///////////

string fileToDownloadName = "Fine";
string? fileId = await gDriveApi.GetFileIdByAsync(fileToDownloadName, sourceFolderId, cts.Token);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fileToDownloadName}.");
    return;
}

string copyFileId;

try
{
    copyFileId = await gDriveApi.CopyFileToAsync(fileId, sourceFolderId, cancellationToken: cts.Token);
}
catch (CopyFileException ex)
{
    Console.WriteLine($"Cannot create a copy of file '{fileToDownloadName}'. Details: {ex.Message}");
    return;
}

/////////// Renaming copy file ///////////

string newFileName = "ItsNotFine";

await gDriveApi.RenameFileAsync(copyFileId, newFileName, cancellationToken: cts.Token);

/////////// Moving copy file ///////////

string destinationFolderName = "1";
string? destinationFolderId = await gDriveApi.GetFolderIdByAsync(destinationFolderName, sourceFolderId, cts.Token);
if (destinationFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {destinationFolderName}.");
    return;
}

await gDriveApi.MoveFileToAsync(copyFileId, sourceFolderId, destinationFolderId, cts.Token);