# Meeting Notes App

**AI-Powered Meeting Note-Taker with Notion Integration**
Capture meeting audio, get live transcription, generate AI summaries, and save structured notes directly to Notion — all running locally on your machine.

---

## Features

### Implemented
- Main interface with Notion database selection and recent notes display
- Note-taking window with live transcription, manual notes, AI summary, key points, and action items
- System audio capture via WASAPI loopback (NAudio)
- Speech-to-text via Windows Speech Recognition (System.Speech)
- AI-powered meeting summaries via LLamaSharp (Private Mode — local, in-process) or cloud API (API Key Mode — user's own key)
- Notion API integration: save notes, fetch recent notes, workspace management
- Settings window: Notion workspace CRUD, AI provider configuration (Private/Cloud), call detection config
- Dark theme UI with muted grey-green accent colors

### Planned
- Real call detection (monitor system processes for Teams, Zoom, Meet, Discord)
- Improved transcription (OpenAI Whisper integration)
- System tray background operation
- Auto-save during meetings
- Desktop notifications for call detection

---

## Privacy & Safety

- **Private Mode (default)** — transcription and AI summaries run entirely on your machine via LLamaSharp
- **API Key Mode (optional)** — use your own OpenAI/Anthropic API key for faster cloud-powered summaries. Only transcript text is sent — audio always stays local.
- **Your Notion, your data** — notes are saved to your own workspace, nothing stored locally
- **No telemetry, no ads, no accounts**

---

## Usage

### Prerequisites
1. .NET 8 SDK
2. Notion API key (create at https://www.notion.so/my-integrations)
3. For Private Mode: first-run download of AI model (~2.49 GB, automatic via Settings)
4. For API Key Mode (optional): API key from OpenAI, Anthropic, or compatible provider

### Running
```
dotnet restore
dotnet build
dotnet run
```

### Configuration
1. Open Settings (gear icon in header)
2. Add a Notion workspace: enter name, paste API key, click "Fetch Databases"
3. Select a database containing "Meetings" in its name
4. Save the workspace
5. Back on the main screen, select a database and click "Start Notes"

---

## Architecture

| Layer | Description |
|-------|-------------|
| **Windows** | WPF windows with code-behind (MainWindow, NoteTakingWindow, SettingsWindow, MeetingSetupWindow) |
| **Models** | MeetingInfo, NotionWorkspaceIntegration, NotionDatabase, KeyPoint, ActionItem |
| **Services** | Notion API (HttpClient), LLamaSharp (Private Mode), Cloud API (API Key Mode), speech recognition (System.Speech), audio capture (NAudio) |
| **Storage** | Notion databases (remote), workspace config (%AppData%/MeetingNotesApp/workspaces.json) |
