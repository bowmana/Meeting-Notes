# roadmap.md

## Meeting Notes App Roadmap

### v0.1 — Basic UI + Partial Backend (Current)

**Goal:** Functional UI with core integrations working.

- Main window with database selection and recent notes
- Note-taking window with all sections (transcription, notes, summary, key points, action items)
- System audio capture via WASAPI loopback (NAudio)
- Speech-to-text via Windows Speech Recognition (System.Speech)
- AI summaries via local LMStudio
- Notion API: save notes with structured properties, fetch recent notes
- Settings: Notion workspace CRUD (add/edit/delete), LMStudio connection test
- Call detection UI toggles (Teams, Zoom, Meet, Discord) — UI only, not functional
- Dark theme with muted grey-green accents
- Workspace persistence to JSON file

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

- Multiple LLM provider support (Ollama, cloud APIs as opt-in)
- Calendar integration (auto-populate meeting info from Outlook/Google Calendar)
- Multi-language transcription
- Meeting analytics (frequency, duration trends)
- Collaborative notes (share meeting link)
- MVVM architecture refactor
