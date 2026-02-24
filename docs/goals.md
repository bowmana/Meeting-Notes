# goals.md

## Product
**Name:** Meeting Notes App
**One-liner:** Capture meeting audio, transcribe it live, generate AI summaries, and save structured notes to Notion — all locally.

## Primary Goals
1. **Frictionless note-taking** — one click to start capturing, no complex setup per meeting
2. **Live transcription** — capture system audio (speakers/headphones) and convert to text in real-time
3. **AI-powered summaries** — generate structured meeting overviews and action items using a local LLM
4. **Notion as the destination** — save notes directly to the user's Notion database with structured properties (title, transcription, notes, summary, key points, action items, date, organizer, attendees)
5. **Privacy-first** — all processing happens locally by default (Private Mode via LLamaSharp). Optional cloud API mode available for users who prefer faster/higher-quality summaries — requires explicit opt-in and uses the user's own API key (BYOK). Audio never leaves the device regardless of mode.

## Target Users
People who:
- Attend frequent meetings (standups, 1:1s, team syncs, client calls)
- Use Notion as their knowledge base or workspace
- Want automated transcription without uploading audio to the cloud
- Need structured meeting records with action items and summaries
- Use Microsoft Teams, Zoom, Google Meet, or Discord for calls

## Current Scope (v0.1 — Basic UI + Partial Backend)
- Main interface with database selection and recent notes
- Note-taking window with all core sections (transcription, notes, summary, key points, action items)
- System audio capture via WASAPI loopback
- Windows Speech Recognition for transcription
- AI summaries via LLamaSharp (Private Mode, local) or cloud API (API Key Mode, opt-in)
- Notion API: save notes, fetch recent notes, workspace CRUD
- Settings: workspace management, AI provider configuration, call detection toggles

## Future Scope
- Real call detection (monitor processes for active calls)
- OpenAI Whisper integration for higher-quality transcription
- System tray operation (background monitoring)
- Auto-save during meetings
- Desktop notifications for detected calls
- Meeting templates (standup, 1:1, retrospective)
- Export to formats beyond Notion (Markdown, PDF)

## Non-Goals
- Cloud-based transcription (privacy boundary)
- Video recording or screen capture
- Calendar integration or meeting scheduling
- Real-time collaboration or shared editing
- Mobile or cross-platform (Windows desktop only for v1)

## Success Metrics
- User can go from "Start Notes" to a saved Notion page in under 2 minutes
- Transcription captures at least the gist of spoken content (Windows Speech Recognition baseline)
- AI summary produces actionable meeting overviews with extracted action items
- In Private Mode: zero data leaves the user's machine (except Notion API writes to their own workspace)
- In API Key Mode: only transcript text (not audio) is sent to the user's chosen cloud provider
