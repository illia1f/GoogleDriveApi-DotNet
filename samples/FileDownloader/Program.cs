using GoogleDriveApi_DotNet;

GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync();

string parentFolderId = "root";
string sourceFolderName = "FileDownloader Test Folder";
string? sourceFolderId = gDriveApi.GetFolderIdBy(sourceFolderName, parentFolderId);
if (sourceFolderId is null)
{
    Console.WriteLine($"Cannot find a folder with a name {sourceFolderName}.");
    return;
}

string fullFileNameToDownload = "Lesson_1.pdf";

string? fileId = gDriveApi.GetFileIdBy(fullFileNameToDownload, sourceFolderId);
if (fileId is null)
{
    Console.WriteLine($"Cannot find a file with a name {fullFileNameToDownload}.");
    return;
}

await gDriveApi.DownloadFileAsync(fileId);