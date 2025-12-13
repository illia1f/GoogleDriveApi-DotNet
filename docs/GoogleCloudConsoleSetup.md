# Google Cloud Console Setup (Google Drive API)

This guide covers **only** the Google Cloud Console steps required to use this library (enable the Google Drive API + create OAuth 2.0 credentials).

## Overview

1. Create a Google Cloud Project
2. Enable the Google Drive API
3. Create OAuth 2.0 credentials and download `credentials.json`

Reference (external): [Example from the internet](https://medium.com/geekculture/upload-files-to-google-drive-with-c-c32d5c8a7abc)

## Create a Google Cloud Project and enable the Google Drive API

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Click the project dropdown at the top and select **New Project**.
3. Enter a project name and click **Create**.
4. Go to **Navigation menu** > **APIs & Services** > **Library**.
5. Search for **Google Drive API** and open it.
6. Click **Enable**.

## Create OAuth 2.0 credentials

1. Go to **Navigation menu** > **APIs & Services** > **Credentials**.
2. Click **Create Credentials** and select **OAuth 2.0 Client IDs**.
3. Configure the OAuth consent screen:
   - Select **External** and click **Create**.
   - Enter the required information (app name, user support email, etc.).
   - Add scopes if needed (often the defaults are fine to start).
   - Click **Save and Continue** until the configuration is complete.
4. On the **Create OAuth client ID** page:
   - Select **Desktop app** or **Web app** as the application type.
   - Click **Create**.
   - Download the JSON file containing your credentials (`credentials.json`).
