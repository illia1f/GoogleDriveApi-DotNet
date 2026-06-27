using GoogleDriveApi_DotNet;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(cancellationToken: cts.Token);

/*
 * Creates a new folder named "NewFolderName" in the root directory and then creates another folder
 * named "NewFolderNameV2" inside the first folder. Then renames the inner folder and moves it up to
 * the root, showing the Folders.RenameAsync and Folders.MoveAsync operations.
 */
{
    string newFolderId = await gDriveApi.Folders.CreateAsync(folderName: "NewFolderName", cancellationToken: cts.Token);

    string newFolderId2 = await gDriveApi.Folders.CreateAsync(folderName: "NewFolderNameV2", parentFolderId: newFolderId, cancellationToken: cts.Token);

    Console.WriteLine("New Folder ID: " + newFolderId);
    Console.WriteLine("New Folder ID2: " + newFolderId2);

    // Rename the inner folder in place, then move it up to the root.
    await gDriveApi.Folders.RenameAsync(newFolderId2, "RenamedFolder", cts.Token);
    Console.WriteLine("Renamed \"NewFolderNameV2\" to \"RenamedFolder\".");

    await gDriveApi.Folders.MoveAsync(newFolderId2, sourceFolderId: newFolderId, destinationFolderId: "root", cancellationToken: cts.Token);
    Console.WriteLine("Moved \"RenamedFolder\" up to the root folder.");
}

Console.WriteLine(new string('-', 50));

/*
 * Gets the folder ID of a specific folder named "Test Folder". 
 */
{
    // Retrieves the folder ID of "Test Folder" in the root directory
    string? folderId = await gDriveApi.Folders.FindIdByNameAsync("Test Folder", cancellationToken: cts.Token);
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
    var folders = await gDriveApi.Folders.ListAsync(parentFolderId: "root", cancellationToken: cts.Token);

    for (int i = 0; i < folders.Count; i++)
    {
        var folder = folders[i];

        Console.WriteLine($"{i + 1}. [{folder.Name}] with ID({folder.Id})");

        // Retrieves a list of subfolders within the current folder
        var subFolders = await gDriveApi.Folders.ListAsync(folder.Id, cancellationToken: cts.Token);
        for (int j = 0; j < subFolders.Count; j++)
        {
            var subFolder = subFolders[j];

            Console.WriteLine($"---|{j + 1}. [{subFolder.Name}] with ID({subFolder.Id})");
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
        string? folderId = await gDriveApi.Folders.FindIdByNameAsync("Test Folder", cancellationToken: cts.Token);
        if (folderId is null)
        {
            Console.WriteLine("Cannot find the Test Folder.");
        }
        else
        {
            await gDriveApi.Folders.DeleteAsync(folderId, cts.Token);
            Console.WriteLine("Test Folder has been deleted =)");
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
