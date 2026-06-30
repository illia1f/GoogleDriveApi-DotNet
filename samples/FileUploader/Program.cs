using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Exceptions;
using GoogleDriveApi_DotNet.Types;
using MimeMapping;

// Example: Using CancellationToken with timeout for authorization
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(immediateAuthorization: false);

// If immediateAuthorization is false, it is necessary to invoke the AuthorizeAsync method.
// Default value is true.
await gDriveApi.AuthorizeAsync(cts.Token);

string filePath = "Files/Escanor.jpg";

if (!File.Exists(filePath))
{
    Console.WriteLine($"Sample file \"{filePath}\" not found.");
    Console.WriteLine("The \"Files\" folder is excluded from git, so you need to provide your own file:");
    Console.WriteLine($"  1. Create the folder samples/FileUploader/Files");
    Console.WriteLine($"  2. Put any .jpg image named \"Escanor.jpg\" there (or adjust \"filePath\" above)");
    Console.WriteLine($"  3. Rebuild and run again");
    return;
}

try
{
    // Uploads a file to Google Drive using a file path (into the root folder).
    string fileId = await gDriveApi.Transfers.UploadAsync(filePath, KnownMimeTypes.Jpeg, cancellationToken: cts.Token);

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");

    // Uploads directly into a target folder by passing parentFolderId —
    // no need to upload to root and move the file afterwards.
    const string folderName = "FileUploaderSample";
    DriveItem? folder = await gDriveApi.Folders.FindFirstByNameAsync(folderName, cancellationToken: cts.Token);
    string folderId = folder?.Id ?? await gDriveApi.Folders.CreateAsync(folderName, cancellationToken: cts.Token);

    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    // Uploads a file to Google Drive using a Stream.
    string streamFileId = await gDriveApi.Transfers.UploadAsync(stream, fileName, KnownMimeTypes.Jpeg, folderId, cts.Token);

    Console.WriteLine($"File has been successfuly uploded into folder \"{folderName}\" with ID({streamFileId})");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled or timed out.");
}
catch (UploadFileException ex)
{
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}