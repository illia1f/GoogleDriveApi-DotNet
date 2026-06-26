# Roadmap

This document outlines the current state of the library and planned features for upcoming releases.

## Current Status

The library is in **active development** and approaching its first stable release. Core functionality for authentication, file uploads/downloads, and folder management is implemented. A v1 architecture redesign (operation-group split, unified domain model, multiple auth providers, and Dependency Injection) is planned before the first NuGet publish — see [Architecture](docs/contributing/architecture.md).

---

## v1.0.0 — Initial Release

### File Operations

- [x] Upload file from path (`Transfers.UploadAsync`)
- [x] Upload file from stream (`Transfers.UploadAsync`)
- [x] Download binary files (`Transfers.DownloadAsync`)
- [x] Download/export Google Workspace files (Docs, Sheets, Slides)
- [x] Get file ID by name (`Files.FindIdByNameAsync`)
- [x] Get files by folder (`Files.ListAsync`)
- [x] Delete file (`Files.DeleteAsync`)
- [x] Move file to different folder (`Files.MoveAsync`)
- [x] Rename file (`Files.RenameAsync`)
- [x] Copy file (`Files.CopyAsync`)
- [x] Update existing file content (`Transfers.UpdateContentAsync`)

### Folder Operations

- [x] Create folder (`Folders.CreateAsync`)
- [x] Delete folder permanently (`Folders.DeleteAsync`)
- [x] Get folder ID by name (`Folders.FindIdByNameAsync`)
- [x] Get folders by parent (`Folders.ListAsync`)
- [x] Get all folders (`Folders.ListAllAsync`)
- [x] Move folder to different parent (`Folders.MoveAsync`)
- [x] Rename folder (`Folders.RenameAsync`)

### Trash Operations

- [x] Move file/folder to trash (`Trash.TrashAsync`)
- [x] Restore file/folder from trash (`Trash.RestoreAsync`)
- [x] Empty trash (`Trash.EmptyAsync`)
- [x] List trashed items (`Trash.ListAsync`)

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
- [ ] Stream-destination downloads (`Transfers.DownloadAsync(fileId, Stream)`, `OpenReadAsync`) + `ExportFileAsync` for Workspace files
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
