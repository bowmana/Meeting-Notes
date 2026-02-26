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
- **Not Connected state** (no workspaces/databases configured): Shows "Connect to Notion" prompt with plain-language explanation and a "Connect Notion" button that opens Settings directly
- **Connected state** (databases available): Shows green connection indicator with workspace name, "Save notes to:" database dropdown, and "Start Meeting" button
- Validation error shown inline (red text) if user clicks "Start Meeting" without selecting a database
- Error clears automatically when user selects a database

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
- "Save to Notion" — creates a new page in the selected Notion database with all fields
- "Generate Summary" — triggers AI summarization via configured provider (Private Mode or API Key Mode)

### Notion Save Properties
| Notion Property | Source |
|----------------|--------|
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

---

## Meeting Setup Window (`MeetingSetupWindow.xaml`)

Optional pre-meeting configuration (currently bypassed — main window goes directly to NoteTakingWindow).

### Workspace Selection
- Dropdown to choose which Notion workspace to save notes to
- Populated from configured workspace integrations

### Meeting Information Form
- Date picker
- Title text input
- Organizer text input
- Attendees text input (comma-separated)
- Comments text area

### Actions
- "Cancel" — closes window
- "Start Taking Notes" — validates workspace + title, creates MeetingInfo, opens NoteTakingWindow

---

## Settings Window (`SettingsWindow.xaml`)

Configuration hub for all integrations and preferences.

### Notion Workspace Integrations
- "Add Workspace" button — shows the add/edit form
- Workspace list showing: name, ID, selected database, connection status
- Per-workspace actions: Edit, Test, Delete
- Add/Edit form:
  - Workspace display name
  - Notion API key (PasswordBox)
  - "Fetch Databases" button — queries Notion API, populates database dropdown (filtered to names containing "Meetings")
  - Database dropdown for selecting target database
  - Save / Cancel buttons
- Workspaces persist to `%AppData%/MeetingNotesApp/workspaces.json`

### AI Settings

#### AI Mode Toggle
- **Private Mode (Local)** — default, all AI processing on-device via LLamaSharp
- **API Key Mode (Cloud)** — opt-in, uses user's own API key for cloud LLM provider

#### Private Mode Section (shown when Private Mode selected)
- Model status: "Downloaded" / "Not downloaded" / "Downloading... X%"
- "Download Model" button (downloads Phi-4-mini Q4_K_M, 2.49 GB, from Hugging Face)
- Download progress bar with cancel button
- GPU detection status: "NVIDIA GPU detected — using GPU acceleration" or "No GPU detected — using CPU (summaries may take 15-30 seconds)"
- "Test Local AI" button — runs a short test inference, shows success/failure dialog
- Performance note: "Private Mode keeps all data on your device. Summaries take ~15-30s on CPU, ~3s with GPU."

#### API Key Mode Section (shown when API Key Mode selected)
- Privacy disclosure banner: "In API Key Mode, your meeting transcript text (not audio) is sent to the selected cloud provider for summarization. Audio always stays on your device."
- Provider dropdown: OpenAI, Anthropic, Custom Endpoint
- API key input (PasswordBox)
- Model name field (e.g., "gpt-4o-mini", "claude-haiku-4-5")
- Custom endpoint URL field (shown only when "Custom Endpoint" selected)
- "Test Cloud AI" button — sends test prompt to configured endpoint, shows success/failure dialog
- Performance note: "API Key Mode sends transcript text to the cloud. Summaries are faster and higher quality, but data leaves your device."

### Call Detection Settings
- Master toggle: Enable/disable automatic call detection
- Per-app toggles: Microsoft Teams, Zoom, Google Meet, Discord
- Each shows enabled state and monitoring status

### General Settings
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
