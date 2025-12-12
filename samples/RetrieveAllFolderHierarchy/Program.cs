using GoogleDriveApi_DotNet;
using GoogleDriveApi_DotNet.Models;
using System.Text;

Console.OutputEncoding = Encoding.UTF8; // Set the console encoding to UTF-8 for proper display of folder names

// Initialize the Google Drive API with the specified credentials and token folder paths and authorize right away
GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("_metadata")
    .SetApplicationName("QuickFilesLoad")
    .BuildAsync(immediateAuthorization: true);

List<GDriveFile> folders = await gDriveApi.GetAllFoldersAsync();

// Create a dictionary to map folder IDs to their corresponding GDriveFile objects
Dictionary<string, GDriveFile> folderDic = folders.ToDictionary(f => f.Id);

// Build a dictionary to map parent folder IDs to lists of their child folder IDs
Dictionary<string, List<string>> childDic = BuildChildDictionary(folders);

// Print the folder hierarchy starting from the root folders
PrintFolderHierarchy(folderDic, childDic);

static Dictionary<string, List<string>> BuildChildDictionary(List<GDriveFile> folders)
{
    var childDict = new Dictionary<string, List<string>>();

    foreach (var folder in folders)
    {
        foreach (var parentId in folder.ParentIds)
        {
            if (!childDict.ContainsKey(parentId))
            {
                childDict[parentId] = new List<string>();
            }
            childDict[parentId].Add(folder.Id);
        }
    }

    return childDict;
}

static void PrintFolderHierarchy(Dictionary<string, GDriveFile> folderDict, Dictionary<string, List<string>> childDict)
{
    var rootFolders = folderDict.Values.Where(f => f.ParentIds.Count == 0).ToList();
    var visited = new HashSet<string>();

    foreach (var rootFolder in rootFolders)
    {
        PrintFolderHierarchyRecursive(rootFolder, folderDict, childDict, string.Empty, visited);
    }
}

static void PrintFolderHierarchyRecursive(GDriveFile folder, Dictionary<string, GDriveFile> folderDict, Dictionary<string, List<string>> childDict, string indent, HashSet<string> visited)
{
    if (visited.Contains(folder.Id))
    {
        Console.WriteLine($"{indent}{folder.Name} (cycle detected, stopping traversal)");
        return;
    }

    visited.Add(folder.Id);
    Console.WriteLine($"{indent}{folder.Name}");

    if (childDict.ContainsKey(folder.Id))
    {
        foreach (var childId in childDict[folder.Id])
        {
            if (folderDict.ContainsKey(childId))
            {
                PrintFolderHierarchyRecursive(folderDict[childId], folderDict, childDict, indent + "  ", visited);
            }
        }
    }

    visited.Remove(folder.Id); // Allow this node to be visited again in a different traversal path
}