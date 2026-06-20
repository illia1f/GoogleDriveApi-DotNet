# GoogleDriveApi-DotNet

A C# library that simplifies interaction with the Google Drive API. It doesn't cover the entire
API — it wraps the most commonly used endpoints (files, folders, trash, upload/download) behind a
clean, fluent interface.

> 🚧 **Pre-release** — approaching v1.0.0. Not yet published to NuGet; first preview is in progress.
> See the [Roadmap](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/ROADMAP.md) for status.

## Installation

Not yet on NuGet. Clone the repo and reference the project directly:

```bash
git clone https://github.com/Illia1F/GoogleDriveApi-DotNet.git
```

```xml
<ProjectReference Include="path/to/src/GoogleDriveApi-DotNet.csproj" />
```

> **Coming soon** — once the first preview ships, install via:
> `dotnet add package GoogleDriveApi.DotNet --prerelease`
> (dependencies `Google.Apis.Drive.v3` and `MimeMapping` are pulled in transitively).

## Quickstart

```csharp
using GoogleDriveApi_DotNet;
using MimeMapping;

// Authorizes on build (opens a browser the first time).
using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetApplicationName("My Drive App")
    .BuildAsync();

string folderId = await gDriveApi.CreateFolderAsync("My Folder");
string fileId   = await gDriveApi.UploadFilePathAsync("photo.jpg", KnownMimeTypes.Jpeg, folderId);
await gDriveApi.DownloadFileAsync(fileId, "Downloads");
```

You need a Google Cloud project and a `credentials.json` first — see
[Getting Started](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/getting-started.md).

## Documentation

| Guides                                                                                                                 | Reference                                                                                             | Project                                                                                                      |
| ---------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| [Getting Started](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/getting-started.md)                  | [Options](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/reference/options.md)       | [Roadmap](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/ROADMAP.md)                             |
| [Uploading files](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/uploading-files.md)           | [Exceptions](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/reference/exceptions.md) | [Architecture](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/contributing/architecture.md) |
| [Downloading files](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/downloading-files.md)       | [MIME types](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/reference/mime-types.md) | [Decision records](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/adr/README.md)            |
| [Managing files](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/managing-files.md)             |                                                                                                       | [Contributing](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/CONTRIBUTING.md)                   |
| [Folders & hierarchy](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/folders-and-hierarchy.md) |                                                                                                       |                                                                                                              |
| [Trash](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/trash.md)                               |                                                                                                       |                                                                                                              |
| [Token & auth](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/docs/guides/token-and-auth.md)               |                                                                                                       |                                                                                                              |

> This library is not a full reflection of the real Google Drive API — it implements the most
> commonly used endpoints to simplify everyday interaction with Google Drive.

## Samples

Runnable projects in [samples/](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples):

- **Console:** [FileUploader](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileUploader),
  [FileDownloader](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileDownloader),
  [FileManagement](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FileManagement),
  [FolderManagement](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/FolderManagement),
  [TrashOperations](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/TrashOperations),
  [RetrieveAllFolderHierarchy](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/RetrieveAllFolderHierarchy)
- **Desktop:** [GDriveExplorerWinForms](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/GDriveExplorerWinForms) —
  a WinForms Drive explorer covering nearly the whole API surface.

> To run a sample, place your `credentials.json` in `samples/Shared`. To share more files across
> samples, add them to the `Directory.Build.targets` config.

## License

MIT — see [LICENSE](https://github.com/Illia1F/GoogleDriveApi-DotNet/blob/main/LICENSE).

## Acknowledgements

- [Google Drive API](https://developers.google.com/drive)
- [Google API .NET Client Library](https://github.com/googleapis/google-api-dotnet-client)
