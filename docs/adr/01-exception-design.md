# ADR 01: Exception Design

- **Status:** Accepted
- **Date:** 2026-06-06
- **Author:** [Illia1F](https://github.com/Illia1F)

## Context

This library wraps the Google Drive v3 API. Callers need to know _what to catch_ and _what to do_
when something fails. Two facts drove the design.

**Fact 1 — exceptions have categories with different purposes.** Litmus test at the API boundary:
_"What should the caller do?"_

| Category     | Examples                                                                 | Caller's move          |
| ------------ | ------------------------------------------------------------------------ | ---------------------- |
| Usage error  | `ArgumentException`, `ObjectDisposedException`, `AuthorizationException` | Fix the code           |
| Operational  | API rejection, network, disk                                             | Retry, skip, or report |
| Control flow | `OperationCanceledException`                                             | Nothing; pass through  |

**Fact 2 — the Google client has two opposite failure mechanisms** (verified against
[Src/Generated/Google.Apis.Drive.v3](https://github.com/googleapis/google-api-dotnet-client/tree/main/Src/Generated/Google.Apis.Drive.v3)
and [Src/Support/Google.Apis](https://github.com/googleapis/google-api-dotnet-client/tree/main/Src/Support/Google.Apis)):

- **Metadata calls** (`request.ExecuteAsync()`): **throw** `GoogleApiException`, carrying
  `HttpStatusCode` and `Error.Errors[].Reason` (`"notFound"`, `"userRateLimitExceeded"`, …).
  Already rich, typed, actionable.
- **Content transfer** (`MediaDownloader.DownloadAsync()` / `ResumableUpload.UploadAsync()`):
  **do not throw.** They return a progress object with `Status = Failed` and the error in
  `.Exception`. Left alone, a failed download "succeeds" and writes an empty file.

Half the API throws a great exception; the other half throws nothing at all.

## Decision

**Wrap by mechanism, not by symmetry:**

1. **Single-`ExecuteAsync` methods** (rename, move, copy, trash, restore, create/delete folder,
   empty trash, all `Get*` reads) throw `GoogleApiException` directly. No wrappers.
2. **Content-transfer methods** (`DownloadFileAsync`, `UploadFilePathAsync`,
   `UploadFileStreamAsync`, `UpdateFileContentAsync`) wrap operational failures into one custom
   exception each (`DownloadFileException`, `UploadFileException`, `UpdateFileContentException`),
   original cause in `InnerException`.
3. **Semantic guards** get their own types only where recovery differs:
   `InvalidMimeTypeException` (folder ID passed to a file operation, or vice versa),
   `UnsupportedMimeTypeException` (server-side MIME type cannot be exported/downloaded).
4. **All custom exceptions derive from `GoogleDriveApiException`** — the one-block escape hatch.
5. **Usage errors and `OperationCanceledException` always pass through.** Every wrap uses a filter:

   ```csharp
   catch (Exception ex) when (ex is not OperationCanceledException and not UploadFileException)
   {
       throw new UploadFileException($"Failed to upload file '{fileName}'.", ex);
   }
   ```

## Rejected alternatives

**A. Wrap every operation** (`RenameFileException`, `TrashFileException`, …): for a single-call
method, `GoogleApiException` already carries everything recovery needs (`404` → skip, `429` →
back off). A wrapper buries that behind `InnerException` and adds zero information — nothing
else can fail. It just multiplies types without distinct meaning.

**B. Wrap nothing:** impossible for transfers — `DownloadAsync`/`UploadAsync` don't throw, so the
wrappers _are_ the failure-surfacing mechanism. Transfers also mix in local file I/O, whose
failure set is unbounded (`IOException`, `UnauthorizedAccessException`, …); "one operation, one
exception type, cause inside" is the only catchable contract.

**Exposing `GoogleApiException` (third-party type) is deliberate:** this library openly is a
Google Drive wrapper; hiding that would be a false abstraction. A generic "cloud storage" facade
would decide the other way.

## Consequences

- **+** Callers branch on `HttpStatusCode`/`Reason` — the richest information available.
- **+** Transfer failures can never be silent; no enumerating I/O exception types.
- **+** Bugs and cancellation surface immediately instead of being retried as "failures".
- **−** Public contract references `Google.GoogleApiException`; switching the underlying client
  would be breaking. Accepted (see above).
- **−** Multi-step methods need two or three catch types — the taxonomy doing its job;
  `GoogleDriveApiException` remains the one-block fallback.

## Rules of thumb (for future methods)

1. One `ExecuteAsync` call, nothing else? → no wrapper; document `GoogleApiException`.
2. Media transfer or local file I/O? → wrap the operational phase into one `<Operation>Exception`
   with `InnerException`; keep usage errors and the metadata `ExecuteAsync` _outside_ the try.
3. New exception type only if callers would **handle it differently** — never for symmetry.
4. Every catch-and-wrap filters out `OperationCanceledException` and never swallows.
5. Document only what survives to the boundary; the docs are the contract.
