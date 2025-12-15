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

string fullFileNameToDownload = "Lesson_1.pdf";

string? fileId = gDriveApi.GetFileIdBy(fullFileNameToDownload, sourceFolderId, cts.Token);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fullFileNameToDownload}.");
    return;
}

await gDriveApi.DownloadFileAsync(fileId, cancellationToken: cts.Token);