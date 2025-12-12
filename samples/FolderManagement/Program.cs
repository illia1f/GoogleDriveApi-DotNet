using GoogleDriveApi_DotNet;

GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync();

/*
 * Creates a new folder named "NewFolderName" in the root directory and then creates another folder
 * named "NewFolderNameV2" inside the first folder.
 */
{
    string newFolderId = gDriveApi.CreateFolder(folderName: "NewFolderName");

    string newFolderId2 = gDriveApi.CreateFolder(folderName: "NewFolderNameV2", parentFolderId: newFolderId);

    Console.WriteLine("New Folder ID: " + newFolderId);
    Console.WriteLine("New Folder ID2: " + newFolderId2);
}

Console.WriteLine(new string('-', 50));

/*
 * Gets the folder ID of a specific folder named "Test Folder". 
 */
{
    // Retrieves the folder ID of "Test Folder" in the root directory
    string? folderId = gDriveApi.GetFolderIdBy("Test Folder");
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
    var folders = gDriveApi.GetFoldersBy(parentFolderId: "root");

    for (int i = 0; i < folders.Count; i++)
    {
        var folder = folders[i];

        Console.WriteLine($"{i + 1}. [{folder.name}] with ID({folder.id})");

        // Retrieves a list of subfolders within the current folder
        var subFolders = gDriveApi.GetFoldersBy(folder.id);
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
        string? folderId = gDriveApi.GetFolderIdBy("Test Folder");
        if (folderId is null)
        {
            Console.WriteLine("Cannot find the Test Folder.");
        }
        else if (gDriveApi.DeleteFolder(folderId))
        {
            Console.WriteLine("Test Folder has been deleted =)");
        }
        else
        {
            Console.WriteLine("Sth went wrong :(");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
