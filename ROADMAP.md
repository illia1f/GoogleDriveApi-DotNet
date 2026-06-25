# Roadmap

This document outlines the current state of the library and planned features for upcoming releases.

## Current Status

The library is in **active development** and approaching its first stable release. Core functionality for authentication, file uploads/downloads, and folder management is implemented. A v1 architecture redesign (operation-group split, unified domain model, multiple auth providers, and Dependency Injection) is planned before the first NuGet publish — see [Architecture](docs/contributing/architecture.md).

---

## v1.0.0 — Initial Release

### File Operations

- [x] Upload file from path (`UploadFilePathAsync`)
- [x] Upload file from stream (`UploadFileStreamAsync`)
- [x] Download binary files (`DownloadFileAsync`)
- [x] Download/export Google Workspace files (Docs, Sheets, Slides)
- [x] Get file ID by name (`GetFileIdByAsync`)
- [x] Get files by folder (`GetFilesByAsync`)
- [x] Delete file (`DeleteFileAsync`)
- [x] Move file to different folder (`MoveFileToAsync`)
- [x] Rename file (`RenameFileAsync`)
- [x] Copy file (`CopyFileToAsync`)
- [x] Update existing file content (`UpdateFileContentAsync`)

### Folder Operations

- [x] Create folder (`CreateFolderAsync`)
- [x] Delete folder permanently (`DeleteFolderAsync`)
- [x] Get folder ID by name (`GetFolderIdByAsync`)
- [x] Get folders by parent (`GetFoldersByAsync`)
- [x] Get all folders (`GetAllFoldersAsync`)
- [x] Move folder to different parent (`Folders.MoveAsync`)
- [x] Rename folder (`Folders.RenameAsync`)

### Trash Operations

- [x] Move file/folder to trash (`MoveFileToTrashAsync`)
- [x] Restore file/folder from trash (`RestoreFileFromTrashAsync`)
- [x] Empty trash (`EmptyTrashAsync`)
- [x] List trashed items (`GetTrashedFilesAsync`)

### Helpers & Utilities

- [x] MIME type detection and mapping
- [x] Google Workspace MIME type handling
- [x] Fluent builder pattern for configuration

### Architecture, Packaging & DI (v1 redesign)

- [x] Operation-group split (`Files` / `Folders` / `Transfers` / `Trash`) over a thin `GoogleDriveApi` facade
- [ ] Unified `DriveItem` domain model (replaces the raw Google type / `GDriveFile` struct / tuples)
- [ ] `IAsyncEnumerable<GDriveItem>` streaming variants for listings
- [ ] Multiple authentication providers: interactive (desktop), service account, stored refresh token
- [ ] Lazy + idempotent + thread-safe authorization (safe for DI singletons)
- [ ] Stream-destination downloads (`DownloadFileAsync(fileId, Stream)`, `OpenReadAsync`) + `ExportFileAsync` for Workspace files
- [ ] `IProgress<long>` progress reporting on transfers _(pulled forward from v1.1.0)_
- [ ] `GoogleDriveApi.Extensions.DependencyInjection` package (`AddGoogleDrive`, options pattern, lifetime knob)
- [ ] NuGet packaging metadata + `GenerateDocumentationFile` + SourceLink (target: `net10.0`)

---

## v1.1.0 — Enhanced Features (Planned)

### Search

- [ ] Advanced search with Google Drive query syntax
- [ ] Search by MIME type
- [ ] Search by date range
- [ ] Search in specific folder

### Progress Reporting

- [ ] `IProgress<long>` for upload operations (bytes uploaded)
- [ ] `IProgress<long>` for download operations (bytes downloaded)
- [ ] Upload/download progress percentage

### File Metadata

- [ ] Get file metadata (size, created date, modified date)
- [ ] Update file metadata
- [ ] Get file permissions
- [ ] Share file with users

### Batch Operations

- [ ] Batch delete multiple files
- [ ] Batch move multiple files
- [ ] Batch download multiple files

---

## Contributing

We welcome contributions! If you'd like to help implement any of the planned features, please:

1. Check our [Contributing Guidelines](CONTRIBUTING.md)
2. Open an issue to discuss the feature
3. Submit a pull request

See the [GitHub Issues](../../issues) for current tasks and feature requests.
