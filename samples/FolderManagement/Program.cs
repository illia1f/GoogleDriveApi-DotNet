using GoogleDriveApi_DotNet;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

/*
 * Creates a new folder named "NewFolderName" in the root directory and then creates another folder
 * named "NewFolderNameV2" inside the first folder.
 */
{
    string newFolderId = await gDriveApi.CreateFolderAsync(folderName: "NewFolderName", cancellationToken: cts.Token);

    string newFolderId2 = await gDriveApi.CreateFolderAsync(folderName: "NewFolderNameV2", parentFolderId: newFolderId, cancellationToken: cts.Token);

    Console.WriteLine("New Folder ID: " + newFolderId);
    Console.WriteLine("New Folder ID2: " + newFolderId2);
}

Console.WriteLine(new string('-', 50));

/*
 * Gets the folder ID of a specific folder named "Test Folder". 
 */
{
    // Retrieves the folder ID of "Test Folder" in the root directory
    string? folderId = await gDriveApi.GetFolderIdByAsync("Test Folder", cancellationToken: cts.Token);
    if (folderId is null)
    {
        Console.WriteLine($"Cannot find a folder.");
    }
    else
    {
        Console.WriteLine($"Folder with ID({folderId}).");
    }
}

Console.WriteLine(new string('-', 50));

/*
 * This block retrieves and prints all folders in the root directory
 * and their subfolders.
 */
{
    // Retrieves a list of folders in the root directory
    var folders = await gDriveApi.GetFoldersByAsync(parentFolderId: "root", cancellationToken: cts.Token);

    for (int i = 0; i < folders.Count; i++)
    {
        var folder = folders[i];

        Console.WriteLine($"{i + 1}. [{folder.name}] with ID({folder.id})");

        // Retrieves a list of subfolders within the current folder
        var subFolders = await gDriveApi.GetFoldersByAsync(folder.id, cancellationToken: cts.Token);
        for (int j = 0; j < subFolders.Count; j++)
        {
            var subFolder = subFolders[j];

            Console.WriteLine($"---|{j + 1}. [{subFolder.name}] with ID({subFolder.id})");
        }
    }
}

Console.WriteLine(new string('-', 50));

/*
 * Deletes the Test Folder
 */
{
    try
    {
        string? folderId = await gDriveApi.GetFolderIdByAsync("Test Folder", cancellationToken: cts.Token);
        if (folderId is null)
        {
            Console.WriteLine("Cannot find the Test Folder.");
        }
        else if (await gDriveApi.DeleteFolderAsync(folderId, cts.Token))
        {
            Console.WriteLine("Test Folder has been deleted =)");
        }
        else
        {
            Console.WriteLine("Sth went wrong :(");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operation was cancelled or timed out.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
