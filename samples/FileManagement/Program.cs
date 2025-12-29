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

/////////// Renaming File ///////////

string fileNameToDownload = "Fine";

string? fileId = gDriveApi.GetFileIdBy(fileNameToDownload, sourceFolderId, cts.Token);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fileNameToDownload}.");
    return;
}

string newFileName = "ItsNotFine";

await gDriveApi.RenameFileAsync(fileId, newFileName, cancellationToken: cts.Token);

/////////// Moving File ///////////

string destinationFolderName = "1";
string? destinationFolderId = gDriveApi.GetFolderIdBy(destinationFolderName, sourceFolderId, cts.Token);
if (destinationFolderId is null)
{
    Console.WriteLine($"Cannot find a file with a name {destinationFolderName}.");
    return;
}
await gDriveApi.MoveFileToAsync(fileId, sourceFolderId, destinationFolderId, cts.Token);