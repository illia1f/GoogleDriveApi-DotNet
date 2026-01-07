# GoogleDriveApi Decoupled Architecture Design

## Goal

Transform the 935-line monolithic `GoogleDriveApi.cs` into a modular, testable service-based architecture while maintaining 100% backward compatibility.

## Architecture Diagram

```
┌─────────────────────────────────────┐
│   GoogleDriveApi (Facade Pattern)   │  ← Backward compatible public API
│   - Delegates to service interfaces │
│   - Provides both facade & services │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  TokenRefreshProxy (DispatchProxy)  │  ← AOP interception via [RefreshToken]
│  - Intercepts marked methods        │
│  - Calls TryRefreshTokenAsync()     │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│       Service Interfaces             │
│  ┌─────────────────────────────────┐│
│  │ IFileOperations                  ││  ← File upload/download/manage
│  │ IFolderOperations                ││  ← Folder create/delete/search
│  │ ITrashOperations                 ││  ← Trash move/restore
│  │ IAuthenticationOperations        ││  ← Auth & token refresh
│  └─────────────────────────────────┘│
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Service Implementations            │
│   (Pure business logic)              │
│   - FileOperations                   │
│   - FolderOperations                 │
│   - TrashOperations                  │
│   - AuthenticationOperations         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Google.Apis.Drive.v3.DriveService │  ← No wrapper, direct usage
└─────────────────────────────────────┘
```

## Key Components

### 1. Service Interfaces (in `src/Abstractions/`)

**IFileOperations** - File management operations

- Upload (path/stream), Download, Rename, Move, Copy, Delete, Search

**IFolderOperations** - Folder management operations

- Create, Delete, Search, List folders

**ITrashOperations** - Trash operations

- Move to trash, Restore from trash, Empty trash, List trashed items

**IAuthenticationOperations** - Authentication & token management

- Authorize, RefreshToken, IsAuthorized, Provider access

### 2. Service Implementations (in `src/Services/`)

Pure business logic classes implementing interfaces:

- `FileOperations.cs` - Implements IFileOperations (~300 lines)
- `FolderOperations.cs` - Implements IFolderOperations (~200 lines)
- `TrashOperations.cs` - Implements ITrashOperations (~100 lines)
- `AuthenticationOperations.cs` - Implements IAuthenticationOperations

All use `GoogleDriveServiceContext` for DriveService access.

### 3. Token Refresh via Attributes

**RefreshTokenAttribute** (in `src/Attributes/`)

```csharp
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RefreshTokenAttribute : Attribute { }
```

**TokenRefreshProxy** (in `src/Services/`)

```csharp
internal sealed class TokenRefreshProxy<T> : DispatchProxy
{
    // Intercepts methods marked with [RefreshToken]
    // Calls TryRefreshTokenAsync() before execution
    // Uses built-in .NET DispatchProxy (no dependencies)
}
```

**Usage in interfaces:**

```csharp
public interface IFileOperations
{
    [RefreshToken]  // ✨ Automatic token refresh
    Task RenameFileAsync(string fileId, string newName, CancellationToken ct = default);

    [RefreshToken]
    Task<string?> GetFileIdByAsync(string fullFileName, string? parentFolderId = null, CancellationToken ct = default);
}
```

### 4. GoogleDriveApi Facade

**Dual API approach:**

```csharp
public class GoogleDriveApi : IDisposable
{
    // Service properties for direct access (NEW)
    public IFileOperations Files { get; }
    public IFolderOperations Folders { get; }
    public ITrashOperations Trash { get; }

    // Facade methods (EXISTING - backward compatible)
    public Task RenameFileAsync(string fileId, string newName, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return Files.RenameFileAsync(fileId, newName, ct);  // Clean delegation
    }
}
```

**Service initialization (after authorization):**

```csharp
private void InitializeServices()
{
    var context = new GoogleDriveServiceContext(_driveService, _options);

    var fileOps = new FileOperations(context);
    var folderOps = new FolderOperations(context);
    var trashOps = new TrashOperations(context);

    // Wrap with AOP proxy for token refresh
    Files = TokenRefreshProxy<IFileOperations>.Create(fileOps, _authOperations);
    Folders = TokenRefreshProxy<IFolderOperations>.Create(folderOps, _authOperations);
    Trash = TokenRefreshProxy<ITrashOperations>.Create(trashOps, _authOperations);
}
```

## Implementation Summary

### Files to Create (11 files)

**Abstractions:**

1. `src/Abstractions/IFileOperations.cs` - File operations interface
2. `src/Abstractions/IFolderOperations.cs` - Folder operations interface
3. `src/Abstractions/ITrashOperations.cs` - Trash operations interface
4. `src/Abstractions/IAuthenticationOperations.cs` - Auth operations interface

**AOP Infrastructure:** 5. `src/Attributes/RefreshTokenAttribute.cs` - Attribute for marking methods 6. `src/Services/TokenRefreshProxy.cs` - DispatchProxy implementation

**Service Implementations:** 7. `src/Services/GoogleDriveServiceContext.cs` - Shared context 8. `src/Services/AuthenticationOperations.cs` - Auth implementation 9. `src/Services/FileOperations.cs` - File operations 10. `src/Services/FolderOperations.cs` - Folder operations 11. `src/Services/TrashOperations.cs` - Trash operations

### Files to Modify (1 file)

1. **`src/GoogleDriveApi.cs`** - Convert to facade pattern
   - Add service properties (Files, Folders, Trash)
   - Replace direct implementation with delegation
   - Initialize services with proxies after authorization

## Usage Examples

### Backward Compatible (Facade)

```csharp
using var api = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .BuildAsync();

// Use existing methods (unchanged)
await api.RenameFileAsync(fileId, "new-name.txt");
await api.CreateFolderAsync("My Folder");
```

### Direct Service Access (New)

```csharp
using var api = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .BuildAsync();

// Use services directly
await api.Files.RenameFileAsync(fileId, "new-name.txt");
await api.Folders.CreateFolderAsync("My Folder");
await api.Trash.MoveFileToTrashAsync(fileId);
```

### Dependency Injection (New)

```csharp
// Register services
services.AddGoogleDriveApi(opts => {
    opts.CredentialsPath = "credentials.json";
});

// Inject specific service
public class MyService
{
    private readonly IFileOperations _fileOps;

    public MyService(IFileOperations fileOps)
    {
        _fileOps = fileOps;
    }

    public Task UploadAsync(string path) =>
        _fileOps.UploadFilePathAsync(path, "application/pdf");
}
```
