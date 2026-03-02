# Meeting Notes App

**AI-Powered Meeting Note-Taker with Multi-Provider Integration**
Capture meeting audio, get live transcription with speaker diarization, generate AI summaries, and save structured notes to Notion, CSV, Excel, and more — all running locally on your machine.

---

## Features

### Implemented
- Main interface with integration selector dropdown, dynamic connection status, and recent notes display
- Note-taking window with live transcription, speaker identification, manual notes, AI summary, key points, and action items
- System audio capture via WASAPI loopback (NAudio)
- Multi-model speech recognition via sherpa-onnx (Moonshine Tiny/Base, Whisper tiny.en/base.en/small.en)
- Speaker diarization via sherpa-onnx (pyannote segmentation + 3D-Speaker embeddings)
- Speaker identification: manual labeling, LLM-based name inference, voice fingerprinting
- AI-powered meeting summaries via LLamaSharp (Private Mode — local, in-process) or cloud API (API Key Mode — user's own key)
- Multi-provider integration architecture: Notion (functional), with coming-soon support for CSV, Excel, Markdown, PDF, Google Drive, OneDrive, Confluence, Slack, and Webhook
- Save service extraction: IMeetingSaveService interface with NotionSaveService implementation
- Settings window with sidebar navigation: Integrations, Speech & Audio, AI, General
- Dark theme UI with muted grey-green accent colors

### Planned
- Real call detection (monitor system processes for Teams, Zoom, Meet, Discord)
- System tray background operation
- Auto-save during meetings
- Desktop notifications for call detection
- Local export providers (CSV, Excel, Markdown, PDF)
- Cloud integrations (Google Drive, OneDrive, Confluence, Slack, Webhook)

---

## Privacy & Safety

- **Private Mode (default)** — transcription and AI summaries run entirely on your machine via LLamaSharp
- **API Key Mode (optional)** — use your own OpenAI/Anthropic API key for faster cloud-powered summaries. Only transcript text is sent — audio always stays local.
- **Your data, your destination** — notes are saved to your chosen integration provider, nothing stored locally beyond the session
- **No telemetry, no ads, no accounts**

---

## Usage

### Prerequisites
1. .NET 8 SDK
2. For Notion: API key (create at https://www.notion.so/my-integrations)
3. For transcription: download at least one ASR model via Settings > Speech & Audio
4. For Private Mode AI: first-run download of AI model (~2.49 GB, automatic via Settings)
5. For API Key Mode (optional): API key from OpenAI, Anthropic, or compatible provider

### Running
```
dotnet restore
dotnet build
dotnet run
```

### Configuration
1. Open Settings (gear icon in header)
2. Go to Integrations page, click "+ Add Integration"
3. Select a provider (e.g., Notion) from the provider picker
4. Configure the integration (API key, target database, etc.)
5. Save the integration
6. Back on the main screen, select the integration and click "Start Meeting"

---

## Architecture

| Layer | Description |
|-------|-------------|
| **Windows** | WPF windows with code-behind (MainWindow, NoteTakingWindow, SettingsWindow, LLamaSharpDebugWindow) |
| **Models** | Integration (base), NotionIntegration, CsvExportIntegration, ExcelExportIntegration, SerializableIntegration, NotionDatabase, MeetingInfo |
| **Services** | IMeetingSaveService (NotionSaveService, CsvSaveService, ExcelSaveService), Notion API (HttpClient), LLamaSharp (Private Mode), Cloud API (API Key Mode), sherpa-onnx ASR + diarization, NAudio |
| **Storage** | Integration config (%AppData%/MeetingNotesApp/integrations.json), app settings (appsettings.json), speaker profiles (speaker_profiles.json) |
