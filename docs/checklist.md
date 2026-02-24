# Meeting Notes App Development Checklist

> This file tracks all development progress. Never recreate — only update items or append new ones. Do not delete any items so we can track progress.
> - `[ ]` Not started
> - `[~]` In progress
> - `[x]` Completed

---

## v0.1 — Basic UI + Partial Backend

### Project Setup
- [x] Project setup (.NET 8 WPF, csproj)
- [x] App.xaml with merged resource dictionaries
- [x] Color system (Styles/Colors.xaml)
- [x] Shared styles (Styles/Styles.xaml)
- [x] App icon (meetingnotes.ico)

### Main Window
- [x] Header with app title and settings gear icon
- [x] Status bar with color-coded indicator
- [x] Database selection dropdown (populated from all workspaces)
- [x] "Start Notes" button → opens NoteTakingWindow
- [x] Redesign "Save Note" → "Start a Meeting" with two-state UX (empty state + connected state), connection indicator, validation errors, and "Connect Notion" button
- [x] Test functions section (Simulate Call, Test Notion, Start Meeting)
- [x] Recent Notes list (fetched from Notion API)
- [x] Click recent note → opens Notion page in browser

### Note-Taking Window
- [x] Meeting information section (title, organizer, attendees, database, start time, duration, status)
- [x] Live transcription section with status indicator
- [x] Start/Stop recording toggle button
- [x] Manual notes text area
- [x] AI summary section (read-only)
- [x] Key points checklist with add functionality
- [x] Action items checklist with add functionality and assignees
- [x] Bottom action buttons (Stop Recording, Save to Notion, Generate Summary)
- [x] Duration timer (live updating)

### Audio Capture & Transcription
- [x] WASAPI loopback capture setup (NAudio)
- [x] System audio recording (speakers/headphones output)
- [x] Audio format conversion (to 16kHz mono WAV for speech recognition)
- [x] Windows Speech Recognition integration (System.Speech)
- [x] Dictation grammar for free-form speech
- [x] Speech recognized → append to transcription area
- [x] Recording state management (start/stop)

### AI Summarization (v0.1 — LMStudio, replaced in v0.1.5)
- [x] LMStudio integration via OpenAI-compatible API (replaced by LLamaSharp + Cloud API in v0.1.5)
- [x] Structured prompt for meeting overview + action items
- [x] Generate summary from transcription + manual notes
- [x] Manual notes appended to summary output

### Notion Integration
- [x] Save meeting notes to Notion (POST /v1/pages)
- [x] Property mapping: Title, Transcription, Notes, AI Summary, Key Points, Action Items, Duration, Organizer, Attendees, Date
- [x] Fetch recent notes from Notion (POST /v1/databases/{id}/query)
- [x] Display recent notes with title, date, preview

### Settings Window
- [x] Notion workspace add/edit/delete
- [x] API key input (PasswordBox)
- [x] Fetch databases from Notion API (search, shows all databases — "Meeting" sorted to top)
- [x] Database selection dropdown
- [x] Workspace persistence (JSON file)
- [x] LMStudio connection test button (replaced by AI provider tests in v0.1.5)
- [x] Call detection master toggle
- [x] Per-app detection toggles (Teams, Zoom, Meet, Discord)
- [x] General settings checkboxes (start minimized, auto-save, notifications)
- [x] Add helper text and Notion integration link to Add Workspace form for better onboarding UX

### Meeting Setup Window
- [x] Workspace selection dropdown
- [x] Meeting info form (date, title, organizer, attendees, comments)
- [x] Start Taking Notes / Cancel buttons

---

## v0.1.5 — AI Provider Architecture

### Private Mode (LLamaSharp — Local AI)
- [x] Add LLamaSharp NuGet package (core)
- [x] Add LLamaSharp.Backend.Cpu NuGet package
- [x] Add LLamaSharp.Backend.Cuda12 NuGet package
- [x] LLamaSharp debug panel (LLamaSharpDebugWindow — model loading, StatelessExecutor inference, streaming output)
- [~] Implement model download manager (download Phi-4-mini Q4_K_M GGUF from Hugging Face to %LocalAppData%/models/)
- [x] Model download UI in debug panel (progress bar, cancel button, download status, inline banner with state transitions)
- [x] Notion sync from debug panel (structured output parsing → Notion blocks: heading_2, bulleted_list_item, to_do, paragraph with 2000-char splitting)
- [ ] Implement `IAiSummaryService` interface (provider-agnostic abstraction)
- [ ] Implement `LLamaSharpSummaryService` (load model, `StatelessExecutor`, stream tokens via `InferAsync`)
- [ ] GPU auto-detection via `NativeLibraryConfig.WithCuda(true).WithAutoFallback(true)`
- [ ] Singleton model lifecycle (lazy load on first summary, dispose on app exit)
- [ ] Handle long transcripts (chunked summarization for transcripts exceeding ~12,000 chars)
- [ ] Streaming token output to AI Summary text area during generation

### API Key Mode (Cloud — Opt-In)
- [ ] Implement `CloudApiSummaryService` (OpenAI-compatible HTTP calls with user API key)
- [ ] Provider dropdown support (OpenAI, Anthropic, custom endpoint)
- [ ] API key input UI (PasswordBox, stored in appsettings.json)
- [ ] Privacy disclosure banner when user selects API Key Mode

### Settings & Infrastructure
- [ ] AI Provider settings section in Settings window (Private Mode / API Key Mode toggle)
- [ ] Private Mode UI: model download status, GPU detection status, "Test Local AI" button
- [ ] API Key Mode UI: provider dropdown, API key field, model name field, "Test Cloud AI" button
- [ ] Persist AI settings to appsettings.json (mode, provider, API key, model path, GPU preference)
- [ ] Replace LMStudio HTTP calls in NoteTakingWindow with `IAiSummaryService`
- [ ] Remove LMStudio-specific test button (replace with generic "Test AI Connection")
- [ ] Remove `SummarizeTextFallback()` — no fallback logic per CLAUDE.md rules

---

## v0.2 — Transcription Improvements

- [ ] OpenAI Whisper integration
- [ ] Real-time streaming transcription
- [ ] Speaker diarization (basic)
- [ ] Handle long recordings

---

## v0.3 — Call Detection

- [ ] Process monitoring for active calls
- [ ] System audio state detection
- [ ] Popup notification on call detection
- [ ] Auto-start/stop recording

---

## v0.4 — System Tray + Background

- [ ] System tray icon
- [ ] Minimize to tray
- [ ] Tray context menu
- [ ] Start on boot
- [ ] Desktop notifications

---

## v0.5 — Enhanced Notion Integration

- [ ] Database auto-creation
- [ ] Block-level content writing
- [ ] Handle 2000-char rich_text limit
- [ ] Meeting templates
- [ ] Rate limit handling

---

## Documentation

- [x] README.md (project root)
- [x] CLAUDE.md (AI agent instructions)
- [x] docs/README.md
- [x] docs/goals.md
- [x] docs/techstack.md
- [x] docs/features.md
- [x] docs/datamodels.md
- [x] docs/ui.md
- [x] docs/niche.md
- [x] docs/marketing.md
- [x] docs/roadmap.md
- [x] docs/checklist.md
- [x] docs/folder_filestructure.md
- [x] docs/lessons.md
- [x] docs/dev-workflow.md
