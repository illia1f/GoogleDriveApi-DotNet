# Google.Apis.Drive.v3 Package

This document provides an overview of the underlying Google.Apis.Drive.v3 NuGet package that this library is built upon.

## Overview

The [Google.Apis.Drive.v3](https://www.nuget.org/packages/Google.Apis.Drive.v3) package is the official .NET client library for the Google Drive API v3, maintained by Google. It provides programmatic access to Google Drive functionality including file management, sharing, collaboration, and metadata operations.

**Official Documentation:** [Google.Apis.Drive.v3 API Reference](https://googleapis.dev/dotnet/Google.Apis.Drive.v3/latest/api/Google.Apis.Drive.v3.html)

## How This Library Uses the Package

This library (`GoogleDriveApi-DotNet`) wraps the `Google.Apis.Drive.v3` package to provide a simplified, more intuitive API for common Google Drive operations. Instead of working directly with the lower-level resources and request builders, you can use high-level methods like:

- `Transfers.UploadAsync()` (from a path or a stream) instead of manually configuring `FilesResource.CreateMediaUpload`
- `Transfers.DownloadAsync()` instead of handling `FilesResource.Get` with media download
- `Folders.CreateAsync()` instead of manually setting MIME types and parent folders

The underlying package handles authentication, request serialization, and API communication, while this library focuses on providing a developer-friendly interface.

## Additional Resources

- [Google Drive API Documentation](https://developers.google.com/drive)
- [Google API .NET Client Library](https://github.com/googleapis/google-api-dotnet-client)
- [Generated Drive v3 Client Source](https://github.com/googleapis/google-api-dotnet-client/tree/main/Src/Generated/Google.Apis.Drive.v3)
- [NuGet Package](https://www.nuget.org/packages/Google.Apis.Drive.v3)
