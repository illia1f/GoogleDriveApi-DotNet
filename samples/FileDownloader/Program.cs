using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Types;

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

string fullFileNameToDownload = "Escanor.jpg";

DriveItem? file = await gDriveApi.Files.FindFirstByNameAsync(fullFileNameToDownload, sourceFolder.Id, cts.Token);
if (file is null)
{
    Console.WriteLine($"Cannot find a file with a name {fullFileNameToDownload}.");
    return;
}

await gDriveApi.Transfers.DownloadAsync(file.Id, cancellationToken: cts.Token);