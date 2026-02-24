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
- Flow: Start Recording → WASAPI captures system audio → Stop Recording → Audio converted to 16kHz mono WAV → System.Speech processes → Text appended to transcription area

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
| Transcription | Live Transcription text |
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
