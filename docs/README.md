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
- AI-powered meeting summaries via local LMStudio (meta-llama-3.1-8b-instruct)
- Notion API integration: save notes, fetch recent notes, workspace management
- Settings window: Notion workspace CRUD, LMStudio connection test, call detection config
- Dark theme UI with muted grey-green accent colors

### Planned
- Real call detection (monitor system processes for Teams, Zoom, Meet, Discord)
- Improved transcription (OpenAI Whisper integration)
- System tray background operation
- Auto-save during meetings
- Desktop notifications for call detection

---

## Privacy & Safety

- **100% local processing** — transcription and AI summaries run on your machine
- **No cloud services** — no data sent to external APIs (except Notion for saving notes)
- **Your Notion, your data** — notes are saved to your own workspace, nothing stored locally
- **No telemetry, no ads, no accounts**

---

## Usage

### Prerequisites
1. .NET 8 SDK
2. Notion API key (create at https://www.notion.so/my-integrations)
3. LMStudio running locally on http://127.0.0.1:1234 with meta-llama-3.1-8b-instruct loaded

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
| **Services** | Notion API (inline HttpClient), speech recognition (System.Speech), audio capture (NAudio) |
| **Storage** | Notion databases (remote), workspace config (%AppData%/MeetingNotesApp/workspaces.json) |
