# GoogleDriveApi-DotNet

This C# library simplifies interaction with the Google Drive API. While it doesn't cover the entire API, it includes the most commonly used endpoints for easier access to Google Drive.

## Features

- Extract folder ID from a Google Drive folder URL.
- Get the folder name by its ID.
- Generate Google Drive folder URL by its id.
- Obtain the full path of a folder starting from the root using its ID.
- Retrieve all folders from Google Drive and print the folder hierarchy. Docs: [Understanding Google Drive Folders and Cycle Dependencies](docs/FolderHierarchyAndCycleDependencies.md)
- Create/Delete folders in Google Drive.
- Check if the access token is expired.
- Refresh the access token if expired.
- Download files from Google Drive. Docs: [Downloading Files from Google Drive](docs/DownloadingFiles.md)
- Upload files to Google Drive.
- Delete files and folders in Google Drive. (Not yet)

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

## Sample code snippets

### Creating an Instance of GoogleDriveApi

First, create an instance of the `GoogleDriveApi` class using the fluent builder pattern with your credentials and token paths:

```csharp
GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
	.SetCredentialsPath("credentials.json")
	.SetTokenFolderPath("_metadata")
	.SetApplicationName("[Your App Name]")
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
