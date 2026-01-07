# Roadmap

This document outlines the current state of the library and planned features for upcoming releases.

## Current Status

The library is in **active development** and approaching its first stable release. Core functionality for authentication, file uploads/downloads, and folder management is implemented.

---

## v1.0.0 — Initial Release

### 🔐 Authentication Operations

- [x] `AuthorizeAsync()` - OAuth 2.0 authentication with Google
- [x] `TryRefreshTokenAsync()` - Refresh authentication token when stale
- [x] `IsAuthorized` - Property to check authorization status
- [x] `IsTokenShouldBeRefreshed` - Property to check if token needs refresh
- [x] Token persistence using FileDataStore
- [x] Configurable credentials and token paths

### 📄 File Operations

**Upload:**
- [x] `UploadFilePath()` - Upload file from local path *(to be converted to async)*
- [x] `UploadFileStream()` - Upload file from stream *(to be converted to async)*

**Download:**
- [x] `DownloadFileAsync()` - Download binary files and Google Workspace files (Docs, Sheets, Slides, Drawings)

**Management:**
- [x] `GetFileIdByAsync()` - Find file ID by name in parent folder
- [x] `RenameFileAsync()` - Rename a file
- [x] `MoveFileToAsync()` - Move file to different folder
- [x] `CopyFileToAsync()` - Copy file to destination folder
- [x] `DeleteFileAsync()` - Permanently delete a file

**Planned:**
- [ ] `UpdateFileContentAsync()` - Update existing file content
- [ ] `GetFileMetadataAsync()` - Retrieve file metadata

### 📁 Folder Operations

**Creation & Deletion:**
- [x] `CreateFolderAsync()` - Create new folder in parent folder
- [x] `DeleteFolderAsync()` - Permanently delete a folder

**Search & Retrieval:**
- [x] `GetFolderIdByAsync()` - Find folder ID by name in parent folder
- [x] `GetFoldersByAsync()` - List folders within parent folder (paginated)
- [x] `GetAllFoldersAsync()` - Retrieve all folders from Drive

**Planned:**
- [ ] `MoveFolderToAsync()` - Move folder to different parent
- [ ] `RenameFolderAsync()` - Rename a folder

### 🗑️ Trash Operations

- [x] `MoveFileToTrashAsync()` - Move file to trash (soft delete)
- [x] `RestoreFileFromTrashAsync()` - Restore file from trash

**Planned:**
- [ ] `EmptyTrashAsync()` - Empty entire trash
- [ ] `ListTrashedItemsAsync()` - List all trashed items

### ⚙️ Configuration & Builder

- [x] `CreateBuilder()` - Create fluent builder for API configuration
- [x] `SetCredentialsPath()` - Configure OAuth credentials path
- [x] `SetTokenFolderPath()` - Configure token storage location
- [x] `SetUserId()` - Set user identifier for token cache
- [x] `SetApplicationName()` - Set application name for API requests
- [x] `SetRootFolderId()` - Set default root folder ID
- [x] `SetAuthProvider()` - Set custom authentication provider
- [x] `BuildAsync()` - Build and optionally authorize API instance

### 🛠️ Helpers & Utilities

- [x] MIME type detection and mapping
- [x] Google Workspace MIME type handling and export
- [x] Custom exception types for error handling
- [x] IDisposable pattern for resource management

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
