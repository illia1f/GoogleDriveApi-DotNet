using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Types;

// To see the difference use debugger

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
DriveItem? sourceFolder = await gDriveApi.Folders.FindFirstByNameAsync(sourceFolderName, parentFolderId, cts.Token);
if (sourceFolder is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

string fileToDeleteName = "Fine";
DriveItem? fileToDelete = await gDriveApi.Files.FindFirstByNameAsync(fileToDeleteName, sourceFolder.Id, cts.Token);
if (fileToDelete is null)
{
    Console.WriteLine("Cannot find file.");
    return;
}

await gDriveApi.Trash.TrashAsync(fileToDelete.Id, cts.Token);

var trashedFiles = await gDriveApi.Trash.ListAsync(cancellationToken: cts.Token);

Console.WriteLine($"Files in trash: {trashedFiles.Count}");

await gDriveApi.Trash.RestoreAsync(fileToDelete.Id, cts.Token);