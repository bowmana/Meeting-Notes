# roadmap.md

## Meeting Notes App Roadmap

### v0.1 — Basic UI + Partial Backend (Current)

**Goal:** Functional UI with core integrations working.

- Main window with database selection and recent notes
- Note-taking window with all sections (transcription, notes, summary, key points, action items)
- System audio capture via WASAPI loopback (NAudio)
- Speech-to-text via Windows Speech Recognition (System.Speech)
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

### v0.2 — Transcription Improvements

- OpenAI Whisper integration for higher-accuracy transcription
- Real-time streaming transcription (instead of record → stop → process)
- Speaker diarization (basic — identify speaker changes)
- Transcription confidence display
- Handle long recordings without memory issues

---

### v0.3 — Call Detection

- Process monitoring for active calls (Teams, Zoom, Meet, Discord)
- System audio state detection (is audio actively playing?)
- Popup notification when call detected: "Take notes for this meeting?"
- Auto-start recording when user confirms
- Auto-stop suggestion when call ends

---

### v0.4 — System Tray + Background Operation

- System tray icon with status indicator
- Run in background, minimize to tray
- Tray context menu: Start Notes, Settings, Recent Notes, Exit
- Start on boot toggle
- Desktop notifications for detected calls

---

### v0.5 — Enhanced Notion Integration

- Notion database auto-creation (set up "Meetings" database if none exists)
- Block-level content writing (instead of flat rich_text for transcription/summary)
- Handle rich_text 2000-char limit by splitting into multiple blocks
- Notion page templates (standup, 1:1, retrospective)
- Improved error handling for rate limits and auth failures

---

### v0.6 — Quality of Life

- Auto-save during meetings (periodic Notion saves)
- Meeting templates with pre-filled key points and action item categories
- Export options beyond Notion (Markdown, plain text)
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
