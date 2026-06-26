using GoogleDriveApi_DotNet;

// To see the difference use debugger

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = await gDriveApi.Folders.FindIdByNameAsync(sourceFolderName, parentFolderId, cts.Token);
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

string fileToDeleteName = "Fine";
string? fileToDeleteId = await gDriveApi.Files.FindIdByNameAsync(fileToDeleteName, sourceFolderId, cts.Token);
if (fileToDeleteId is null)
{
    Console.WriteLine("Cannot find file.");
    return;
}

await gDriveApi.Trash.TrashAsync(fileToDeleteId, cts.Token);

var trashedFiles = await gDriveApi.Trash.ListAsync(cancellationToken: cts.Token);

Console.WriteLine($"Files in trash: {trashedFiles.Count}");

await gDriveApi.Trash.RestoreAsync(fileToDeleteId, cts.Token);