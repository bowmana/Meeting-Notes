# Meeting Notes App Feature Specifications

---

## Main Window (`MainWindow.xaml`)

The primary interface. Shows monitoring status, database selection, test functions, and recent notes.

### Header
- App title "Meeting Notes" with green accent header bar
- Settings gear icon (opens SettingsWindow)

### Status Bar
- Color-coded status indicator (gray=Ready, green=Monitoring, blue=Recording)
- Status text + description (e.g., "Monitoring — Listening for calls from Teams, Zoom, and Meet")

### Start a Meeting Section
Two-state design with progressive disclosure:
- **Not Connected state** (no integrations configured): Shows "Get Started" prompt with subtitle "Choose where to save your meeting notes — Notion, CSV, Excel, and more." and an "Add Integration" button that opens Settings → Integrations page
- **Connected state** (integrations available): Shows connection indicator with integration info, "Save notes to:" integration selector dropdown (each item shows `[Provider Badge] Display Name — Target`), and "Start Meeting" button
- Integration selector dropdown items display provider badge, display name, and target description (e.g., `[N] Work Notion — Sprint Planning DB`)
- Status text is dynamic based on selected integration provider (e.g., "Connected to Notion", "CSV export ready")
- Validation error shown inline (red text) if user clicks "Start Meeting" without selecting an integration
- Error clears automatically when user selects an integration

### Test Functions Section
- "Simulate Call Detection" — sets status to Recording
- "Test Notion Connection" — tests API connectivity (enabled only when database picker is available)
- "Start New Meeting" — same as Start Notes

### Recent Notes Section
- ListBox displaying last 10 notes fetched from Notion
- Each item shows: Title, Date, Preview (first 100 chars of notes)
- Clicking a note opens its Notion page in the browser

---

## Note-Taking Window (`NoteTakingWindow.xaml`)

The core note-taking experience. Opens when user starts a new meeting.

### Meeting Information
- Editable fields: Title, Organizer, Attendees
- Read-only fields: Database, Started time, Duration (live timer), Status

### Live Transcription
- Status indicator with color dot (Gray=Ready, Green=Recording/Completed, Orange=Listening/Processing, Red=Error)
- Start/Stop Recording toggle button (Green when idle, Red when recording)
- Scrollable text area showing transcribed speech
- Flow: Start Recording → WASAPI captures system audio → Stop Recording → Audio converted to 16kHz mono WAV → sherpa-onnx diarization → float[] sub-array per segment → sherpa-onnx ASR (user-selected model: Moonshine or Whisper) per segment → Speaker-labeled text appended to transcription area

### Speaker Diarization (Post-Recording)

After recording stops, the full captured audio is analyzed by sherpa-onnx to identify which speaker said what. This is a post-processing step — not real-time.

**Processing Flow:**
1. User clicks "Stop Recording"
2. Audio is converted to 16kHz mono WAV (same as for speech recognition)
3. sherpa-onnx runs offline diarization: segmentation → embedding → clustering
4. Progress bar shows diarization progress with percentage (e.g., "Processing audio... 45%")
5. Detected speaker count displayed (e.g., "3 speakers detected")
6. Full WAV loaded as float[] once; for each speaker segment, a float[] sub-array is indexed by timestamp and fed to the user-selected sherpa-onnx ASR model (Moonshine or Whisper, configurable in Settings) for transcription — no audio file slicing
7. Final transcription shows speaker-labeled text with timestamps

**Transcription Format:**
```
Speaker 1 [0:00 - 0:15]: Hello everyone, welcome to the meeting.
Speaker 2 [0:15 - 0:32]: Thanks for having me. Let's get started.
Speaker 1 [0:32 - 0:48]: Great. First item on the agenda...
```

**UI Elements:**
- Diarization progress bar (visible during processing, hidden otherwise)
- Status text showing current step ("Running speaker diarization...", "Transcribing segments...", "Complete")
- Speaker count badge (e.g., "3 speakers")

**Model Requirement:**
- Requires diarization models to be downloaded (~36 MB) via Settings > Speaker Diarization > Download Models
- Requires at least one ASR model to be downloaded via Settings > Speech Recognition (5 models available: Moonshine Tiny/Base, Whisper tiny.en/base.en/small.en)
- If the selected ASR model is not downloaded: show clear error message. User must download a model in Settings first.
- If diarization models not downloaded: transcription proceeds without speaker labels, with status message "Speaker diarization unavailable — download models in Settings"
- If diarization fails at runtime: clear error shown in status area, no transcript produced for that recording

**Speaker Labels:**
- Generic labels: "Speaker 1", "Speaker 2", etc. (no voice recognition / name matching)
- Labels are consistent within a single recording (Speaker 1 is always the same voice)
- Number of speakers auto-detected by default, or user can set a fixed count in Settings

### Your Notes (Manual)
- Free-form text area for manual notes alongside transcription
- Saved as a separate "Your Notes" field in Notion

### AI Summary
- Read-only text area displaying generated summary
- AI mode indicator showing "Private" (green) or "Cloud" (blue) based on active provider
- "Generate Summary" button sends transcription + manual notes to the configured AI provider
- Tokens stream to the text area in real-time during generation (Private Mode)
- Structured output: Meeting Overview + Action Items

### Key Points
- Checklist of important discussion points
- Add new key points via text input + "Add" button
- Each point has a checkbox for completion tracking
- Initialized with sample items (for demo)

### Action Items
- Checklist of tasks with assignees
- Add new items via text input + "Add" button
- Each item shows: checkbox, text, assignee
- Initialized with sample items (for demo)

### Bottom Actions
- "Stop Recording" — stops transcription timer and audio capture
- **Dynamic save button** — text changes based on the active integration provider:
  - Notion: "Save to Notion"
  - CSV: "Export to CSV"
  - Excel: "Export to Excel"
  - Markdown: "Export to Markdown"
  - PDF: "Export to PDF"
  - Google Drive: "Save to Google Drive"
  - Generic: "Save Notes"
- "Generate Summary" — triggers AI summarization via configured provider (Private Mode or API Key Mode)

### Save Output Fields

All integration providers receive the same meeting data. How it's stored depends on the provider:

| Field | Description |
|-------|-------------|
| Title | Meeting Title field |
| Transcription | Live Transcription text (speaker-labeled when diarization available) |
| Speakers | Number of speakers detected (e.g., "3 speakers detected") |
| My Notes | Manual Notes text |
| AI Summary | Generated summary text |
| Key Points | Bullet list of key points |
| Action Items | Bullet list with assignees |
| Duration | Meeting duration string |
| Organizer | Organizer field |
| Attendees | Attendees field |
| Date | Meeting start time (ISO 8601) |

**Provider-specific mapping:**
- **Notion**: Saves as structured properties (Title, Rich Text, Date) on a Notion database page
- **CSV**: Each field becomes a column in the CSV file
- **Excel**: Each field becomes a column in the spreadsheet
- **Markdown**: Structured headings and sections in a .md file
- **PDF**: Formatted document with sections
- **Google Drive / OneDrive / Confluence**: Provider-specific document format

---

## Settings Window (`SettingsWindow.xaml`)

Configuration hub for all integrations and preferences. Uses a **sidebar navigation** layout with 4 pages.

### Settings Layout: Sidebar Navigation
The Settings window uses a sidebar + content panel layout (similar to VS Code/Discord/Slack settings):
- **Sidebar** (left, ~180px): 4 navigation items — Integrations, Speech & Audio, AI, General
- **Content panel** (right): Shows the selected page's controls
- Active sidebar item has a `PrimaryBrush` left border accent + lighter background
- "Close" button at bottom-right

### Page 1: Integrations

#### Integration List
- "**+ Add Integration**" button — opens the provider picker
- Integration list showing all configured integrations:
  - Provider badge (colored circle with abbreviation: "N" for Notion, "G" for Google Drive, etc.)
  - Integration display name (e.g., "Work Notion")
  - Provider name + target in secondary text (e.g., "Notion · Sprint Planning DB")
  - Status indicator (green dot + "Connected" for cloud, green + "Ready" for local exports)
  - Edit / Delete action buttons (Test button only for cloud providers)
- Integrations persist to `%AppData%/MeetingNotesApp/integrations.json`

#### Provider Picker (shown when "Add Integration" clicked)
A card grid with available integration providers, grouped by category:

**Cloud Services:**
| Provider | Badge Color | Status | Description |
|----------|-------------|--------|-------------|
| Notion | Black (#000) | **Functional** | Save to a Notion database with structured properties |
| Google Drive | Blue (#4285F4) | Coming soon | Save meeting notes as Google Docs in Drive |
| OneDrive/SharePoint | Blue (#0078D4) | Coming soon | Save to OneDrive or SharePoint document libraries |
| Confluence | Blue (#0052CC) | Coming soon | Save to Atlassian Confluence wiki pages |
| Slack | Purple (#4A154B) | Coming soon | Post meeting summaries to Slack channels |

**Local Exports:**
| Provider | Badge Color | Status | Description |
|----------|-------------|--------|-------------|
| CSV | Gray | Coming soon | Export meeting notes as .csv files |
| Excel | Green (#217346) | Coming soon | Export meeting notes as .xlsx spreadsheets |
| Markdown | Gray | Coming soon | Export as .md files (great for Obsidian, wikis) |
| PDF | Red (#FF0000) | Coming soon | Export as formatted .pdf documents |
| Webhook | Gray | Coming soon | Send meeting data to any custom API endpoint |

- Cards: ~150x120px, `SurfaceElevatedBrush` background, 10px corner radius
- Available cards: clickable with `PrimaryBrush` hover border glow
- "Coming soon" cards: `Opacity="0.5"`, not clickable, `WarningBrush` pill badge
- Cancel button returns to integration list

#### Notion Configuration Form (shown after clicking Notion card)
- "< Back" link to return to provider picker
- Quick Setup Guide (same 3-step Notion setup guide, shown only on "Add", hidden on "Edit")
- Display Name text field
- Notion API Key (PasswordBox)
- "Fetch Databases" button — queries Notion API, populates database dropdown
- Target Database dropdown
- "Save Integration" / "Cancel" buttons

#### CSV Export Configuration Form (future)
- Display Name, Export Folder path with Browse button

#### Excel Export Configuration Form (future)
- Display Name, Export Mode (one file per meeting / append to single file), Export path with Browse button

### Page 2: Speech & Audio
Combines the current Speaker Diarization and Speech Recognition sections:

#### Speaker Diarization
- Active segmentation model selector (ComboBox)
- Model list with per-model download/delete buttons, progress bars
- Embedding model download/delete
- Advanced settings: number of speakers, clustering threshold

#### Speech Recognition (ASR)
- Active ASR model selector (ComboBox)
- Model list with per-model download/delete buttons, progress bars

### Page 3: AI

#### AI Mode Toggle
- **Private Mode (Local)** — default, all AI processing on-device via LLamaSharp
- **API Key Mode (Cloud)** — opt-in, uses user's own API key for cloud LLM provider

#### Private Mode Section (shown when Private Mode selected)
- Model status: "Downloaded" / "Not downloaded" / "Downloading... X%"
- "Download Model" button (downloads Phi-4-mini Q4_K_M, 2.49 GB, from Hugging Face)
- Download progress bar with cancel button
- GPU detection status
- "Test Local AI" button
- Performance note

#### API Key Mode Section (shown when API Key Mode selected)
- Privacy disclosure banner
- Provider dropdown: OpenAI, Anthropic, Custom Endpoint
- API key input (PasswordBox)
- Model name field
- Custom endpoint URL field (shown only when "Custom Endpoint" selected)
- "Test Cloud AI" button
- Performance note

### Page 4: General

#### Call Detection Settings
- Master toggle: Enable/disable automatic call detection
- Per-app toggles: Microsoft Teams, Zoom, Google Meet, Discord
- Each shows enabled state and monitoring status

#### Preferences
- Start minimized to system tray (checkbox)
- Automatically save notes during meetings (checkbox)
- Show desktop notifications for call detection (checkbox)

---

## Supported Meeting Platforms (Call Detection)

| Platform | Status |
|----------|--------|
| Microsoft Teams | UI toggle present, detection not yet implemented |
| Zoom | UI toggle present, detection not yet implemented |
| Google Meet | UI toggle present, detection not yet implemented |
| Discord | UI toggle present (disabled by default), detection not yet implemented |
