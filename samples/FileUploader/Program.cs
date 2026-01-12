using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Exceptions;
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

try
{
    // Uploads a file to Google Drive using a file path.
    string fileId = await gDriveApi.UploadFilePathAsync(filePath, KnownMimeTypes.Jpeg, cts.Token);

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");


    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    // Uploads a file to Google Drive using a Stream.
    await gDriveApi.UploadFileStreamAsync(stream, fileName, KnownMimeTypes.Jpeg, cts.Token);

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");
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