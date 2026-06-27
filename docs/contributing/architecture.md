# GoogleDriveApi Architecture

This page describes the architecture the library is being restructured toward for **v1**: a
clean-slate redesign of the public surface, landed before the first NuGet publish locks it under
semver. It also records the current (pre-v1) shape for context. Status and sequencing are tracked
in the [Roadmap](../../ROADMAP.md).

The goal: one codebase that serves desktop apps, headless web APIs, and (via an extension point)
per-user web apps, with first-class Dependency Injection, while keeping non-DI usage
(console/WinForms) dependency-light.

---

## Today (pre-v1)

A single `GoogleDriveApi` class (~1150 lines) holds every operation: files, folders, trash,
upload/download. It is simple to follow but has grown into a god class: a copy-pasted pagination
loop, mixed concerns, and a two-phase `Create` + `AuthorizeAsync` lifecycle that throws on
resolve-before-auth and on double-auth (awkward for DI).

Two patterns carry forward into v1:

- **`Provider` property as the single guard point.** It runs `ThrowIfDisposed()` and the
  "not authorized" check, so individual methods don't repeat them.

  ```csharp
  public DriveService Provider
  {
      get
      {
          ThrowIfDisposed();
          return _service ?? throw new AuthorizationException("...");
      }
  }
  ```

- **Wrap-by-mechanism exception design** (see [ADR-01](../adr/01-exception-design.md)). The
  redesign carries this taxonomy intact.

---

## Target architecture (v1)

### Two packages

Core stays DI-free; DI support is a separate opt-in package. Console/WinForms consumers take core
only, with zero `Microsoft.Extensions.*`.

```
┌─────────────────────────────────────────────────────────────┐
│  GoogleDriveApi  (core)                                       │
│  • GDriveClient (facade) + operation groups                  │
│  • GDriveItem domain model, exceptions                       │
│  • IGDriveAuthProvider + 3 built-in providers                │
│  • Builder + IGDriveClientFactory                            │
│  deps: Google.Apis.Drive.v3, MimeMapping        (DI-free)    │
└───────────────────────────┬─────────────────────────────────┘
                            │ referenced by
┌───────────────────────────▼─────────────────────────────────┐
│  GoogleDriveApi.Extensions.DependencyInjection               │
│  • AddGoogleDrive(...) + options binding + lifetime knob      │
│  deps: core, Microsoft.Extensions.DI.Abstractions, .Options  │
└──────────────────────────────────────────────────────────────┘
```

There is **no `*.Abstractions` package**: the contracts intentionally expose Google types (the
`RawService` escape hatch, the credential), so an abstractions package would still drag in
`Google.Apis.Drive.v3` and gain nothing.

### Client + operation-group split

The god class becomes a thin facade over four independently-mockable operation groups. Reads like
the underlying `DriveService.Files.*`:

```
GDriveClient   (facade: auth, Dispose, RawService escape hatch)
  ├─ .Files     : IGDriveFileOperations      (get/rename/move/copy/delete/find/list)
  ├─ .Folders   : IGDriveFolderOperations    (create/delete/rename/move/find/list)
  ├─ .Transfers : IGDriveTransferOperations  (upload/download/export/update-content)
  └─ .Trash     : IGDriveTrashOperations      (trash/restore/empty/list)
```

Each group is an interface → independently mockable and parallel-developable (kills the
merge-conflict god class). The pagination loop (copy-pasted 4×) collapses into one shared
`ListAllPagesAsync(...)` helper plus an `IAsyncEnumerable<GDriveItem>` streaming variant. The
two missing folder methods land here as **validated wrappers** (verify MIME == folder first,
matching `Folders.DeleteAsync`'s safety stance): `Folders.MoveAsync`, `Folders.RenameAsync`.

### Domain model

Three old read shapes (the raw Google type, the `GDriveFile` struct, and `(id, name)` tuples)
collapse into one owned record, `DriveItem`. It is the default return of every list and find:

```csharp
public sealed record DriveItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MimeType MimeType { get; init; }   // value object; owns IsFolder
    public bool IsFolder => MimeType.IsFolder;
}
```

`DriveItem` carries only the core fields every item always has: `id`, `name`, `mimeType`. These are
cheap and always fetched, so a `DriveItem` is always fully populated.

`Size`, `ModifiedTime`, and `ParentIds` are not on the model (a reversal of the earlier "growable
record" sketch). A field that callers can skip for performance cannot also be guaranteed on the
model: once it is optional, an empty value is ambiguous (not present, or just not requested?).
Optional fields live behind `DriveFields` instead.

Variable fields (`parents`, `size`, `modifiedTime`, `owners`, links, and so on) are reached through a
`DriveFields` selector. It drives field-selected overloads that return the raw
`Google.Apis.Drive.v3.Data.File`:

```csharp
// default → DriveItem (id, name, mimeType)
Task<IReadOnlyList<DriveItem>> ListAsync(string? parentFolderId = null, int pageSize = 100, CancellationToken ct = default);
// field-selected → raw File carrying exactly the requested fields
Task<IReadOnlyList<GoogleFile>> ListAsync(string? parentFolderId, DriveFields fields, int pageSize = 100, CancellationToken ct = default);
```

`DriveFields` is immutable and composable (`DriveFields.Default.WithSize().WithModifiedTime()`, plus
`WithRaw(...)` for fields without a dedicated method); it renders the Drive `Fields` mask. The raw
`File` is exposed only at the operation boundary (the `File`-returning overloads and
`Files.FindByIdAsync`), never on the domain model. So `DriveItem` stays immutable and free of Google
types, while full fidelity stays one call away (ADR-01's "openly a wrapper"). The raw `DriveService`
also stays reachable via `client.RawService`.

All overloads share one pagination loop in `DriveServiceExtensions.ListAsync`; the `DriveItem` reads
are a `.Select(ToDriveItem)` over it. Public APIs return `IReadOnlyList<T>`, never `List<T>`.

See the design: [Unified DriveItem + DriveFields](../superpowers/specs/2026-06-27-unified-driveitem-and-fields.md).

### Authentication

The abstraction stops exposing `UserCredential` and unifies all flows behind one provider:

```csharp
public interface IGDriveAuthProvider
{
    // GoogleCredential is the common base for user, service-account, and access-token credentials.
    Task<IConfigurableHttpClientInitializer> GetCredentialAsync(CancellationToken ct = default);
}
```

Three built-in providers (all on `Google.Apis.Auth`, already transitive, so no new dependencies):

| Provider                     | Host         | Notes                                                               |
| ---------------------------- | ------------ | ------------------------------------------------------------------- |
| `InteractiveAuthProvider`    | Desktop      | `GoogleWebAuthorizationBroker` + `FileDataStore` (today's behavior) |
| `ServiceAccountAuthProvider` | Web API      | service-account JSON key, headless                                  |
| `RefreshTokenAuthProvider`   | Web API      | stored refresh token + client secret                                |
| _(user-implemented, scoped)_ | Web per-user | the extension point, not shipped                                    |

Auth settings split out of client options (today `GoogleDriveApiOptions` carries
`CredentialsPath`/`TokenFolderPath`/`UserId` even when a custom provider is supplied, which is
misleading dead state):

```
GDriveOptions             → ApplicationName, RootFolderId, + .Auth selector
  InteractiveAuthOptions  → CredentialsPath, TokenFolderPath, UserId
  ServiceAccountOptions   → KeyPath / KeyJson
  RefreshTokenOptions     → ClientId, ClientSecret, RefreshToken
```

The empty marker `IOptions` interface is removed (it collides with
`Microsoft.Extensions.Options.IOptions<T>`).

### Lifetime & Dependency Injection

The client caches its `DriveService` for **its own DI lifetime**; lifetime choice is the only thing
that differs between hosts:

```
Singleton client → credential cached for app lifetime   (desktop, service account, refresh token)
Scoped client    → credential cached per HTTP request    (per-user, user-wired)
```

Auth becomes **lazy + idempotent + thread-safe** (`SemaphoreSlim`): the first API call authorizes
once; an explicit `AuthorizeAsync` stays for eager auth but "already authorized" is a no-op, not an
exception. This is what makes a DI singleton usable. `IGDriveClientFactory.Create(...)` is the
escape hatch for non-DI background workers iterating over many users.

```csharp
// Hosts 1–3: singleton (default)
services.AddGoogleDrive(o =>
{
    o.ApplicationName = "MyApp";
    o.Auth.UseServiceAccount("sa.json");   // or .UseInteractive(...) / .UseRefreshToken(...)
});

// Per-user: opt into scoped + supply a scoped provider (user-written)
services.AddGoogleDrive(o => o.ApplicationName = "Web", ServiceLifetime.Scoped);
services.AddScoped<IGDriveAuthProvider, MyPerUserAuthProvider>();
```

`AddGoogleDrive` binds via the options pattern and registers the client **and each operation-group
interface**, so consumers inject just the slice they need. The `ServiceLifetime` parameter is the
per-user seam; there is no dedicated `AddGoogleDrivePerUser` sugar in v1.

### Streams as first-class

`IGDriveTransferOperations` exposes an explicit stream surface with a documented ownership
contract, and no `MemoryStream` buffering of whole files:

```csharp
// Download — three shapes, streamed straight to the destination
Task DownloadAsync(string fileId, string saveToPath = "Downloads", ...);  // → disk
Task DownloadAsync(string fileId, Stream destination, ...);               // → caller's stream
Task<Stream> OpenReadAsync(string fileId, ...);                               // → stream caller disposes

// Google Workspace export needs an explicit target type (no single binary form)
Task ExportFileAsync(string fileId, string exportMimeType, Stream destination, ...);
```

- **Ownership contract:** the library never disposes a `Stream` the caller passes in (upload source
  / download destination); `OpenReadAsync` returns a stream the **caller** disposes.
- **Workspace routing:** the disk variant derives filename+extension from the MIME type; stream
  variants can't, so Workspace files (Docs/Sheets/Slides) route through `ExportFileAsync`, and plain
  `DownloadAsync(Stream)` throws `UnsupportedMimeTypeException` for them.
- Transfers gain optional `IProgress<long>` progress reporting (the private upload hook surfaced).

### Naming

The whole public surface unifies on the `GDrive*` prefix (e.g. `GDriveClient`, `GDriveItem`,
`GDriveApiException`), and the namespace drops the underscore: `GoogleDriveApi_DotNet` →
`GoogleDriveApi`.

---

## Constraints preserved (not redesigned)

- **ADR-01 exception design (Accepted):** "wrap by mechanism." The operation-group split carries
  the taxonomy intact: transfer ops keep their custom exceptions (`UploadFileException`,
  `DownloadFileException`, `UpdateFileContentException`), single-`ExecuteAsync` ops keep throwing
  `Google.GoogleApiException` raw, semantic guards keep `InvalidMimeTypeException` /
  `UnsupportedMimeTypeException`, and all custom types stay under the base exception. Only the rename
  (`GoogleDriveApiException` → `GDriveApiException`) changes. See
  [ADR-01](../adr/01-exception-design.md).
- **Token refresh stays automatic,** handled by `Google.Apis`; see
  [token-refresh-internals.md](token-refresh-internals.md).
