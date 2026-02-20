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

### Save Note Section
- Database dropdown (populated from all configured Notion workspaces)
- "Start Notes" button — opens NoteTakingWindow with selected database
- Helper text pointing to Settings for workspace configuration

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
- "Generate Summary" button sends transcription + manual notes to LMStudio
- Structured output: Meeting Overview + Action Items
- Fallback: simple truncation if LMStudio is unavailable

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
- "Generate Summary" — triggers AI summarization via LMStudio

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
- LMStudio configuration info (http://127.0.0.1:1234, meta-llama-3.1-8b-instruct)
- "Test LMStudio Connection" button — sends test prompt, shows success/failure dialog

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
