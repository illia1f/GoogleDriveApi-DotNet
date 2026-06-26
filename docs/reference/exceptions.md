# Exceptions Reference

What this library throws, and what you should do about it. The design rationale lives in
[ADR-01: Exception design](../adr/01-exception-design.md). Per-method exception lists are in
the XML doc comments (IntelliSense); this page is the cross-cutting overview.

## Categories

At the API boundary, ask "what should the caller do?":

- **Usage error** (`ArgumentException`, `ObjectDisposedException`, `AuthorizationException`) — fix the code.
- **Operational** (API rejection, network, disk, transfer failure) — retry, skip, or report.
- **Control flow** (`OperationCanceledException`) — nothing; let it pass.

## The library's design in one line

**Wrap by mechanism, not by symmetry.** Metadata calls already throw a rich, typed
`Google.GoogleApiException` — so those operations expose it directly, unwrapped. Content
transfers (upload/download/export/update-content) do **not** throw on failure in the underlying
library, so this library wraps them into one custom exception each, with the cause in
`InnerException`.

## Type hierarchy

All custom types derive from `GoogleDriveApiException` — catch that as a one-block escape hatch.

```
GoogleDriveApiException            (base — catch to handle any library-specific failure)
├─ AuthorizationException          usage error: not authorized, or authorized twice
├─ InvalidMimeTypeException        semantic guard: wrong item type (e.g. folder id to file op)
├─ UnsupportedMimeTypeException    server-side type can't be exported/downloaded
├─ UploadFileException             upload failed (cause in InnerException)
├─ UpdateFileContentException      content replacement failed (cause in InnerException)
└─ DownloadFileException           download/export failed (cause in InnerException)
```

`Google.GoogleApiException` is **not** part of this hierarchy — it is the third-party type thrown
directly by metadata operations. It carries `HttpStatusCode` and `Error.Errors[].Reason`
(`"notFound"`, `"userRateLimitExceeded"`, …), which is the richest signal to branch on.

## What throws what

- **Single metadata calls** (rename, move, copy, trash, restore, create/delete folder, empty
  trash, `Get*`/`Find*`) — `Google.GoogleApiException` directly; `ArgumentException` for bad input.
- **`Transfers.UploadAsync` / `Transfers.UploadAsync`** — `UploadFileException` (operational),
  `FileNotFoundException` (path overload), `ArgumentException`.
- **`Transfers.UpdateContentAsync`** — `UpdateFileContentException`, `ArgumentException` /
  `ArgumentNullException`.
- **`Transfers.DownloadAsync`** — `DownloadFileException`, `UnsupportedMimeTypeException`,
  `Google.GoogleApiException`, `ArgumentException`.
- **`Files.DeleteAsync` / `Folders.DeleteAsync`** — `InvalidMimeTypeException` when the id is the wrong
  type; otherwise `Google.GoogleApiException`.
- **Before `AuthorizeAsync`, or after `Dispose`** — `AuthorizationException` /
  `ObjectDisposedException`.

## Rules that always hold

- **Usage errors and `OperationCanceledException` always pass through** — never wrapped, never
  swallowed. Every catch-and-wrap filters them out:

  ```csharp
  catch (Exception ex) when (ex is not OperationCanceledException and not UploadFileException)
  {
      throw new UploadFileException($"Failed to upload file '{fileName}'.", ex);
  }
  ```

- **Transfer failures are never silent.** The wrappers exist precisely because the underlying
  `DownloadAsync`/`UploadAsync` return a failed-status object instead of throwing.

## Catching examples

Handle a specific operational failure, then fall back:

```csharp
try
{
    await gDriveApi.Transfers.UploadAsync(path, mimeType);
}
catch (UploadFileException ex)
{
    logger.LogError(ex, "Upload failed; cause: {Cause}", ex.InnerException?.Message);
}
catch (GoogleDriveApiException ex) // any other library-specific failure
{
    logger.LogError(ex, "Drive operation failed");
}
```

Branch on the Google status for metadata calls:

```csharp
try
{
    await gDriveApi.Trash.TrashAsync(fileId);
}
catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
{
    // 404 — skip
}
```
