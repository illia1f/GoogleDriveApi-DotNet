# Getting Started

This is the single onboarding path for the library: set up Google Cloud, drop your
credentials into the app, and make your first authorized call.

It covers two stages:

1. **Google Cloud Console** — enable the Drive API and create OAuth 2.0 credentials.
2. **Application setup** — wire `credentials.json` into your app and authorize.

> **Reference:** [Upload files to Google Drive with C#](https://medium.com/geekculture/upload-files-to-google-drive-with-c-c32d5c8a7abc) (external article)

---

## Stage 1 — Google Cloud Console

You need a Google Cloud project with the Drive API enabled and an OAuth 2.0 client.

### Step 1: Create a Google Cloud project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the **project dropdown** at the top of the page (next to the Google Cloud logo).
3. In the popup window, click **New Project** (top-right corner).
4. Enter a **Project name** (e.g., "My Drive App").
5. Click **Create** and wait for the project to be created.
6. Make sure your new project is selected in the project dropdown.

### Step 2: Enable the Google Drive API

1. In the left sidebar, click **Navigation menu** (☰) > **APIs & Services** > **Library**.
2. In the search bar, type **Google Drive API**.
3. Click on **Google Drive API** in the search results.
4. Click the **Enable** button.

### Step 3: Configure the OAuth consent screen

> **Important:** Configure the OAuth consent screen **before** creating OAuth 2.0 credentials.

1. Go to **Navigation menu** (☰) > **APIs & Services** > **OAuth consent screen**.
2. Select **User Type**:
   - Choose **External** (allows any Google account to use your app).
   - Click **Create**.
3. Fill in the **App information**:
   - **App name**: a name for your application.
   - **User support email**: select your email from the dropdown.
   - **Developer contact information**: your email address.
   - Leave other (optional) fields as default.
4. Click **Save and Continue**.
5. On the **Scopes** page: skip adding scopes for now (they are requested at runtime). Click **Save and Continue**.
6. On the **Summary** page, click **Back to Dashboard**.

### Step 4: Add test users

> **Important:** While your app is in **Testing** mode (not published), only users on the test-users list can authorize. Skip this and you will get an authorization error.

1. Go to **Navigation menu** (☰) > **APIs & Services** > **OAuth consent screen**.
2. Scroll to the **Test users** section.
3. Click **Add Users**.
4. Enter the **email address** of the Google account you will test with.
5. Click **Save**. (Add multiple users individually if needed.)

### Step 5: Create OAuth 2.0 credentials

1. Go to **Navigation menu** (☰) > **APIs & Services** > **Credentials**.
2. Click **Create Credentials** > **OAuth client ID**.
3. **Application type**: select **Desktop app** (for console/desktop applications).
4. Enter a **Name** (e.g., "Desktop Client") and click **Create**.
5. In the popup, click **Download JSON**.
6. Rename the downloaded file to `credentials.json`.

### Troubleshooting (Cloud setup)

| Issue                                           | Solution                                                                        |
| ----------------------------------------------- | ------------------------------------------------------------------------------- |
| "Access blocked: This app's request is invalid" | Complete the OAuth consent screen configuration (Step 3).                       |
| "Error 403: access_denied"                      | Add your Google account to the **Test users** list (Step 4).                    |
| "credentials.json not found"                    | Ensure the file is in the right directory and named exactly `credentials.json`. |

---

## Stage 2 — Application setup

### Step 1: Add `credentials.json` to your project

Place the downloaded `credentials.json` in your project directory (or wherever your app
expects it — the path is configurable, see below).

### Step 2: Create and authorize the client

The library uses a fluent builder. By default it authorizes immediately:

```csharp
using GoogleDriveApi_DotNet;

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json") // default: "credentials.json"
    .SetTokenFolderPath("_metadata")        // default: "_metadata"
    .SetApplicationName("My Drive App")     // optional
    .BuildAsync();

// gDriveApi is now authorized and ready to use.
```

To defer authorization (e.g., to control timing or apply a timeout), pass
`immediateAuthorization: false` and call `AuthorizeAsync` yourself:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

using GoogleDriveApi gDriveApi = await GoogleDriveApi.CreateBuilder()
    .SetCredentialsPath("credentials.json")
    .BuildAsync(immediateAuthorization: false);

await gDriveApi.AuthorizeAsync(cts.Token);
```

### Step 3: First run — browser authorization

The **first** time you run your app, a browser window opens asking you to authorize access
to your Google Drive. After you consent, a token is cached under `SetTokenFolderPath` (default
`_metadata`), so subsequent runs do not prompt again. Token refresh is handled automatically —
see [Token and Auth](guides/token-and-auth.md).

---

## Next steps

- [Uploading files](guides/uploading-files.md)
- [Downloading files](guides/downloading-files.md)
- [Folders and hierarchy](guides/folders-and-hierarchy.md)
- [Trash](guides/trash.md)
- [Token and auth](guides/token-and-auth.md)
- Reference: [Options](reference/options.md) · [Exceptions](reference/exceptions.md) · [MIME types](reference/mime-types.md)
