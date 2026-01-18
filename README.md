# GoogleDriveApi-DotNet

This C# library simplifies interaction with the Google Drive API. While it doesn't cover the entire API, it includes the most commonly used endpoints for easier access to Google Drive.

## Status

🚧 **Pre-release** — The library is in active development approaching v1.0.0.

| Category                             | Status         |
| ------------------------------------ | -------------- |
| Authentication                       | ✅ Complete    |
| File Upload/Download                 | ✅ Complete    |
| Folder Management                    | ✅ Complete    |
| File Management (move, rename, copy) | ✅ Complete    |
| Trash Operations                     | ✅ Complete    |
| Advanced Search                      | 📋 Planned     |

See the full [Roadmap](ROADMAP.md) for detailed feature tracking.

## Documentation

- [Google.Apis.Drive.v3 Package Overview](docs/GoogleApisDrivePackage.md)
- [Downloading Files from Google Drive](docs/DownloadingFiles.md)
- [Understanding Folder Hierarchy and Cycle Dependencies](docs/FolderHierarchyAndCycleDependencies.md)

> **Note:** This library is not a full reflection of the real Google Drive API but implements the most commonly used API endpoints to simplify interaction with Google Drive.

## Installation

Add the Google Drive API NuGet package to your project:

```bash
dotnet add package Google.Apis.Drive.v3
```

> Download this library or create our own implementation based on this template.

## Setup

### Google Cloud Console (enable API + OAuth)

See [Google Cloud Console Setup](docs/GoogleCloudConsoleSetup.md).

### Application setup

1. Place the downloaded `credentials.json` file in your project directory.
2. Initialize the `GoogleDriveApi` class with your credentials.

## If you are developer

> For testing samples just place downloaded `credentials.json` in samples/Shared directory.
> If you want to share more files across sample project include them in `Directory.Build.targets` config file.

## Sample code snippets

### Creating an Instance of GoogleDriveApi

First, create an instance of the `GoogleDriveApi` class using the fluent builder pattern with your credentials and token paths:

```csharp
using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
	.SetCredentialsPath("credentials.json") // default value "credentials.json"
	.SetTokenFolderPath("_metadata") // default value "_metadata"
	.BuildAsync();
```

Additional sample code snippets are available in the [Sample Code Snippets file](docs/SampleCodeSnippets.md).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please read our [Contributing Guidelines](CONTRIBUTING.md) to get started.

## Acknowledgements

- [Google Drive API](https://developers.google.com/drive)
- [Google API .NET Client Library](https://github.com/googleapis/google-api-dotnet-client)
