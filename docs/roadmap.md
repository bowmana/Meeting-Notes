# roadmap.md

## Meeting Notes App Roadmap

### v0.1 — Basic UI + Partial Backend (Current)

**Goal:** Functional UI with core integrations working.

- Main window with database selection and recent notes
- Note-taking window with all sections (transcription, notes, summary, key points, action items)
- System audio capture via WASAPI loopback (NAudio)
- Speech-to-text via Windows Speech Recognition (System.Speech) — replaced by sherpa-onnx Moonshine Tiny ASR in v0.2
- AI summaries via LMStudio (replaced by LLamaSharp/Cloud API in v0.1.5)
- Notion API: save notes with structured properties, fetch recent notes
- Settings: Notion workspace CRUD (add/edit/delete)
- Call detection UI toggles (Teams, Zoom, Meet, Discord) — UI only, not functional
- Dark theme with muted grey-green accents
- Workspace persistence to JSON file

---

### v0.1.5 — AI Provider Architecture

**Goal:** Replace LMStudio dependency with in-process LLamaSharp for zero-config local AI, and add optional cloud API support for users who want faster/higher-quality summaries.

#### Private Mode (LLamaSharp — Local, Default)
- LLamaSharp integration with `StatelessExecutor` for in-process LLM inference
- Model download manager (first-run download of Phi-4-mini Q4_K_M from Hugging Face with progress bar)
- Model stored at `%AppData%/MeetingNotesApp/models/`
- GPU auto-detection (CUDA 12 for NVIDIA GPUs, automatic CPU fallback)
- Streaming token output to UI during summary generation
- Long transcript handling via chunked summarization (context window management)
- Singleton model lifecycle (lazy load on first use, dispose on app exit)

#### API Key Mode (Cloud — Opt-In)
- Cloud API inference service using OpenAI-compatible `/v1/chat/completions` endpoint
- Provider dropdown in Settings: OpenAI, Anthropic, or custom endpoint URL
- User provides their own API key (BYOK — Bring Your Own Key)
- Privacy disclosure shown when user selects cloud mode

#### Settings & Infrastructure
- AI Provider settings UI: mode toggle (Private / API Key), model status, API key input
- "Test AI Connection" button (replaces LMStudio-specific test)
- Persist AI settings to `appsettings.json` (mode, provider, API key, model path)
- `IAiSummaryService` interface with swappable implementations (LLamaSharp + Cloud)
- Remove LMStudio as a hard dependency

---

### v0.2 — Speaker Diarization + Transcription Improvements

**Goal:** Add speaker identification to transcription using sherpa-onnx offline diarization, so the app knows who said what. Improve the transcription pipeline to produce speaker-labeled output that feeds into AI summarization and Notion saving.

- **Speaker diarization via sherpa-onnx** — offline, post-recording speaker identification using pyannote segmentation + 3D-Speaker embedding ONNX models (~36 MB total). Processes full audio after recording stops, identifies speaker turns, produces labeled segments ("Speaker 1 [0:00-0:15]: text...").
- **Diarization model download manager** — download UI in Settings (similar to LLamaSharp model download), model status indicator, ~36 MB one-time download to `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/`
- **Multi-model ASR via sherpa-onnx** — replaced System.Speech with sherpa-onnx OfflineRecognizer. Supports 5 user-selectable models (Moonshine Tiny/Base, Whisper tiny.en/base.en/small.en). All use the same org.k2fsa.sherpa.onnx package — zero additional dependencies. Models downloaded independently in Settings. Eliminated audio file slicing; uses float[] sub-array indexing per speaker segment.
- **Per-segment transcription** — after diarization identifies speaker segments, each segment's audio (as float[] sub-array) is individually fed to sherpa-onnx Moonshine ASR for transcription, producing speaker-attributed text
- **TranscriptionSegment data model** — structured data (SpeakerIndex, SpeakerLabel, Text, StartSeconds, EndSeconds) replaces the flat LiveTranscription string
- **Speaker-aware AI summarization** — LLamaSharp prompt updated to leverage speaker labels for better action item attribution and discussion structure
- **Speaker-labeled Notion transcription** — Notion save includes speaker labels and timestamps, plus a "Speakers" property showing detected count
- **Diarization progress UI** — progress bar and status text in NoteTakingWindow during post-recording processing
- Handle long recordings without memory issues

---

### v0.3 — Multi-Provider Integration Architecture + Settings Overhaul

**Goal:** Transform the app from Notion-only to a multi-provider integration platform with a modern, zero-friction Settings UI.

#### Settings Window Redesign
- Replace scrollable single-page Settings with **sidebar navigation** (4 pages: Integrations, Speech & Audio, AI, General)
- Sidebar styling: 180px fixed width, `PrimaryBrush` active accent, hover states
- Combine Speaker Diarization + ASR into single "Speech & Audio" page
- Combine Call Detection + Preferences into single "General" page

#### Integration Data Models
- Create `Models/` directory with `Integration` abstract base class
- `IntegrationProviderType` enum (Notion, GoogleDrive, OneDrive, Confluence, Slack, CsvExport, ExcelExport, MarkdownExport, PdfExport, Webhook)
- `NotionIntegration : Integration` (replaces `NotionWorkspaceIntegration`)
- `CsvExportIntegration`, `ExcelExportIntegration` (for future use)
- `SerializableIntegration` (replaces `SerializableWorkspace`, with `ProviderType` discriminator)
- Auto-migration from `workspaces.json` to `integrations.json`

#### Provider Picker UI
- "**+ Add Integration**" button opens a provider selection card grid
- 10 providers across 2 categories: Cloud Services (Notion, Google Drive, OneDrive, Confluence, Slack) + Local Exports (CSV, Excel, Markdown, PDF, Webhook)
- Notion is the only functional provider; all others show "Coming soon" badge (dimmed, not clickable)
- Provider-specific configuration forms with "< Back" navigation
- Integration list with provider badges, display name, target description, and status

#### Save Service Extraction
- Create `Services/` directory with `IMeetingSaveService` interface + `MeetingData` DTO
- Extract inline Notion API save logic from `NoteTakingWindow` into `NotionSaveService`
- `CsvSaveService`, `ExcelSaveService` stubs for future providers

#### MainWindow + NoteTakingWindow Updates
- Replace database dropdown with integration selector dropdown
- Dynamic save button text based on selected integration provider
- Update empty state: "Get Started" with "Add Integration" button
- Dynamic status text based on integration provider type

---

### v0.4 — Call Detection

- Process monitoring for active calls (Teams, Zoom, Meet, Discord)
- System audio state detection (is audio actively playing?)
- Popup notification when call detected: "Take notes for this meeting?"
- Auto-start recording when user confirms
- Auto-stop suggestion when call ends

---

### v0.5 — System Tray + Background Operation

- System tray icon with status indicator
- Run in background, minimize to tray
- Tray context menu: Start Notes, Settings, Recent Notes, Exit
- Start on boot toggle
- Desktop notifications for detected calls

---

### v0.6 — Enhanced Notion Integration

- Notion database auto-creation (set up "Meetings" database if none exists)
- Block-level content writing (instead of flat rich_text for transcription/summary)
- Handle rich_text 2000-char limit by splitting into multiple blocks
- Notion page templates (standup, 1:1, retrospective)
- Improved error handling for rate limits and auth failures

---

### v0.7 — Local Export Providers

- **CSV Export**: `CsvSaveService` implementation — one .csv file per meeting
- **Excel Export**: `ExcelSaveService` via ClosedXML — single file or per-meeting mode
- **Markdown Export**: `MarkdownSaveService` — structured .md file with headings
- **PDF Export**: `PdfSaveService` via QuestPDF or itext7 — formatted document
- Remove "Coming soon" badges from implemented providers
- File browser dialogs for export path configuration

---

### v0.8 — Cloud Integrations

- **Google Drive**: OAuth2 flow, save as Google Docs
- **OneDrive/SharePoint**: Microsoft Graph API, save to document libraries
- **Confluence**: Atlassian API, save as wiki pages
- **Slack**: Slack Web API, post summaries to channels
- **Webhook**: HTTP POST with configurable endpoint + headers

---

### v0.9 — Quality of Life

- Auto-save during meetings (periodic saves to active provider)
- Meeting templates with pre-filled key points and action item categories
- Keyboard shortcuts for common actions (start/stop recording, save)
- Improved UI polish and responsive layout

---

### v1.0 — Polish + Release

- Onboarding flow (first-run setup wizard)
- Packaging (MSIX or standalone installer)
- Error logging and diagnostics
- Accessibility improvements
- Performance optimization for long meetings
- User documentation and FAQ

---

### Future Ideas (post-v1)

- Ollama support as additional local inference backend
- Calendar integration (auto-populate meeting info from Outlook/Google Calendar)
- Multi-language transcription
- Meeting analytics (frequency, duration trends)
- Collaborative notes (share meeting link)
- MVVM architecture refactor
- Additional integrations: Airtable, Google Sheets, Email, Microsoft Teams channels
