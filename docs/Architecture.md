# GoogleDriveApi Architecture

## Current Architecture

The `GoogleDriveApi` class provides a simple, direct implementation with all operations in a single class. This design prioritizes:

- **Simplicity** - All code is in one place, easy to understand
- **Debuggability** - Clear stack traces, no proxy indirection
- **Discoverability** - Contributors can follow the code flow immediately

```
┌─────────────────────────────────────┐
│         GoogleDriveApi              │  ← Single public API class
│   - File operations                 │
│   - Folder operations               │
│   - Trash operations                │
│   - Upload/Download helpers         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Google.Apis.Drive.v3.DriveService │  ← Handles token refresh automatically
│   + UserCredential (interceptor)    │     via UserCredential.InterceptAsync()
└─────────────────────────────────────┘
```

## Token Refresh

See [TokenRefresh.md](TokenRefresh.md) for details on how token refresh is handled automatically by Google.Apis.

## Key Design Decisions

### Direct Implementation (No Internal_ Methods)

The class uses direct public method implementations rather than a public/internal split:

```csharp
// Simple, direct implementation
public async Task RenameFileAsync(string fileId, string newName, CancellationToken ct = default)
{
    ArgumentException.ThrowIfNullOrEmpty(fileId);
    ArgumentException.ThrowIfNullOrEmpty(newName);

    var metadata = new GoogleFile { Name = newName };
    var updateRequest = Provider.Files.Update(metadata, fileId);
    updateRequest.Fields = "id,name";

    await updateRequest.ExecuteAsync(ct).ConfigureAwait(false);
}
```

### Disposal Check via Provider Property

The `Provider` property includes the disposal check, so individual methods don't need to call `ThrowIfDisposed()`:

```csharp
public DriveService Provider
{
    get
    {
        ThrowIfDisposed();  // Single check point
        return _service ?? throw new AuthorizationException("...");
    }
}
```

## Usage Examples

### Basic Usage

```csharp
using var api = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .BuildAsync();

await api.RenameFileAsync(fileId, "new-name.txt");
await api.CreateFolderAsync("My Folder");
await api.MoveFileToTrashAsync(fileId);
```

### Custom Configuration

```csharp
using var api = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .SetTokenFolderPath("tokens")
    .SetApplicationName("My App")
    .SetRootFolderId("specific-folder-id")
    .BuildAsync();
```

---

## Future: Service-Based Architecture (Proposed)

For larger codebases or when DI is needed, the monolithic class can be refactored into service interfaces:

```
┌─────────────────────────────────────┐
│   GoogleDriveApi (Facade Pattern)   │  ← Backward compatible public API
│   - Delegates to service interfaces │
│   - Provides both facade & services │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│       Service Interfaces            │
│  ┌─────────────────────────────────┐│
│  │ IFileOperations                 ││  ← File upload/download/manage
│  │ IFolderOperations               ││  ← Folder create/delete/search
│  │ ITrashOperations                ││  ← Trash move/restore
│  └─────────────────────────────────┘│
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Service Implementations           │
│   (Pure business logic)             │
│   - FileOperations                  │
│   - FolderOperations                │
│   - TrashOperations                 │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Google.Apis.Drive.v3.DriveService │
└─────────────────────────────────────┘
```

### Files to Create

**Abstractions:**

1. `src/Abstractions/IFileOperations.cs` - File operations interface
2. `src/Abstractions/IFolderOperations.cs` - Folder operations interface
3. `src/Abstractions/ITrashOperations.cs` - Trash operations interface

**Service Implementations:**

4. `src/Services/GoogleDriveServiceContext.cs` - Shared context
5. `src/Services/FileOperations.cs` - File operations
6. `src/Services/FolderOperations.cs` - Folder operations
7. `src/Services/TrashOperations.cs` - Trash operations

### Dependency Injection Example

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

This refactoring maintains 100% backward compatibility while enabling DI and better testability.
