# Google Cloud Console Setup (Google Drive API)

This guide covers **only** the Google Cloud Console steps required to use this library (enable the Google Drive API + create OAuth 2.0 credentials).

> **Reference**: [Upload files to Google Drive with C#](https://medium.com/geekculture/upload-files-to-google-drive-with-c-c32d5c8a7abc) (external article)

---

## Overview

1. Create a Google Cloud Project
2. Enable the Google Drive API
3. Configure the OAuth consent screen
4. Add test users
5. Create OAuth 2.0 credentials and download `credentials.json`

---

## Step 1: Create a Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the **project dropdown** at the top of the page (next to the Google Cloud logo).
3. In the popup window, click **New Project** (top-right corner).
4. Enter a **Project name** (e.g., "My Drive App").
5. Click **Create** and wait for the project to be created.
6. Make sure your new project is selected in the project dropdown.

---

## Step 2: Enable the Google Drive API

1. In the left sidebar, click **Navigation menu** (☰) > **APIs & Services** > **Library**.
2. In the search bar, type **Google Drive API**.
3. Click on **Google Drive API** in the search results.
4. Click the **Enable** button.

---

## Step 3: Configure the OAuth Consent Screen

> **Important**: You must configure the OAuth consent screen **before** creating OAuth 2.0 credentials.

1. Go to **Navigation menu** (☰) > **APIs & Services** > **OAuth consent screen**.
2. Select **User Type**:
   - Choose **External** (allows any Google account to use your app).
   - Click **Create**.
3. Fill in the **App information**:
   - **App name**: Enter a name for your application.
   - **User support email**: Select your email from the dropdown.
   - **Developer contact information**: Enter your email address.
   - Leave other fields as default (optional fields).
4. Click **Save and Continue**.
5. On the **Scopes** page:
   - You can skip adding scopes for now (they will be requested at runtime).
   - Click **Save and Continue**.
6. On the **Summary** page, click **Back to Dashboard**.

---

## Step 4: Add Test Users

> **Important**: While your app is in **Testing** mode (not published), only users added to the test users list can authorize and use the app. If you skip this step, you will get an authorization error when running the samples.

1. Go to **Navigation menu** (☰) > **APIs & Services** > **OAuth consent screen**.
2. Scroll down to the **Test users** section.
3. Click **Add Users**.
4. Enter the **email address** of the Google account you will use to test the app (e.g., your own Gmail address).
5. Click **Save**.

You can add multiple test users if needed. Each user must be added individually.

---

## Step 5: Create OAuth 2.0 Credentials

1. Go to **Navigation menu** (☰) > **APIs & Services** > **Credentials**.
2. Click **Create Credentials** (top of the page).
3. Select **OAuth client ID**.
4. Choose the **Application type**:
   - Select **Desktop app** for console/desktop applications.
5. Enter a **Name** for the client (e.g., "Desktop Client").
6. Click **Create**.
7. A popup will appear with your client ID and secret. Click **Download JSON**.
8. Rename the downloaded file to `credentials.json`.
9. Place `credentials.json` in your project's root directory (or the location expected by your application).

---

## Troubleshooting

| Issue                                           | Solution                                                                          |
| ----------------------------------------------- | --------------------------------------------------------------------------------- |
| "Access blocked: This app's request is invalid" | Make sure you've completed the OAuth consent screen configuration.                |
| "Error 403: access_denied"                      | Add your Google account email to the **Test users** list (Step 4).                |
| "credentials.json not found"                    | Ensure the file is in the correct directory and named exactly `credentials.json`. |

---

## Next Steps

After completing this setup, you're ready to use the library. The first time you run your application, a browser window will open asking you to authorize access to your Google Drive.
