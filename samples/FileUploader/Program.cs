using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Exceptions;
using MimeMapping;

GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(immediateAuthorization: false);

// If immediateAuthorization is false, it is necessary to invoke the Authorize method.
// Default value is true.
gDriveApi.Authorize();

string filePath = "Files/Escanor.jpg";

try
{
    // Uploads a file to Google Drive using a file path.
    string fileId = gDriveApi.UploadFilePath(filePath, KnownMimeTypes.Jpeg);

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");


    using var stream = new FileStream(filePath, FileMode.Open);
    string fileName = Path.GetFileName(filePath);

    // Uploads a file to Google Drive using a Stream.
    gDriveApi.UploadFileStream(stream, fileName, KnownMimeTypes.Jpeg);

    Console.WriteLine($"File has been successfuly uploded with ID({fileId})");
}
catch (CreateMediaUploadException ex)
{
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}