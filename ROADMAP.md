# Roadmap

This document outlines the current state of the library and planned features for upcoming releases.

## Current Status

The library is in **active development** and approaching its first stable release. Core functionality for authentication, file uploads/downloads, and folder management is implemented.

---

## v1.0.0 — Initial Release

### Authentication & Authorization

- [x] OAuth 2.0 authentication with Google
- [x] Token persistence (FileDataStore)
- [x] Automatic token refresh when stale
- [x] Check if token needs refresh (`IsTokenShouldBeRefreshed`)
- [x] Configurable credentials and token paths

### File Operations

- [x] Upload file from path (`UploadFilePath`)
- [x] Upload file from stream (`UploadFileStream`)
- [x] Download binary files (`DownloadFileAsync`)
- [x] Download/export Google Workspace files (Docs, Sheets, Slides)
- [x] Get file ID by name (`GetFileIdBy`, `GetFileIdByAsync`)
- [ ] Delete file
- [ ] Move file to different folder
- [ ] Rename file
- [ ] Copy file
- [ ] Update existing file content

### Folder Operations

- [x] Create folder (`CreateFolder`, `CreateFolderAsync`)
- [x] Delete folder permanently (`DeleteFolder`, `DeleteFolderAsync`)
- [x] Get folder ID by name (`GetFolderIdBy`, `GetFolderIdByAsync`)
- [x] Get folders by parent (`GetFoldersBy`, `GetFoldersByAsync`)
- [x] Get all folders (`GetAllFolders`, `GetAllFoldersAsync`)
- [ ] Move folder to different parent
- [ ] Rename folder

### Trash Operations

- [ ] Move file/folder to trash (soft delete)
- [ ] Restore file/folder from trash
- [ ] Empty trash
- [ ] List trashed items

### Helpers & Utilities

- [x] MIME type detection and mapping
- [x] Google Workspace MIME type handling
- [x] Fluent builder pattern for configuration

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
