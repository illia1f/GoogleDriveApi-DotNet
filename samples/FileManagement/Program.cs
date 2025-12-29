using GoogleDriveApi_DotNet;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = gDriveApi.GetFolderIdBy(sourceFolderName, parentFolderId, cts.Token);
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

/////////// Creating a copy of file ///////////

string fileToDownloadName = "Fine";
string? fileId = gDriveApi.GetFileIdBy(fileToDownloadName, sourceFolderId, cts.Token);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fileToDownloadName}.");
    return;
}

string? copyFileId = await gDriveApi.CopyFileToAsync(fileId, sourceFolderId, cancellationToken: cts.Token);
if (copyFileId is null)
{
    Console.WriteLine($"Cannot create a copy of file with a name {fileToDownloadName}.");
    return;
}

/////////// Renaming copy file ///////////

string newFileName = "ItsNotFine";

await gDriveApi.RenameFileAsync(copyFileId, newFileName, cancellationToken: cts.Token);

/////////// Moving copy file ///////////

string destinationFolderName = "1";
string? destinationFolderId = gDriveApi.GetFolderIdBy(destinationFolderName, sourceFolderId, cts.Token);
if (destinationFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {destinationFolderName}.");
    return;
}

await gDriveApi.MoveFileToAsync(copyFileId, sourceFolderId, destinationFolderId, cts.Token);