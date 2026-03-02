# goals.md

## Product
**Name:** Meeting Notes App
**One-liner:** Capture meeting audio, transcribe it live, generate AI summaries, and save structured notes to Notion, Google Drive, Excel, and more — all locally.

## Primary Goals
1. **Frictionless note-taking** — one click to start capturing, no complex setup per meeting
2. **Live transcription** — capture system audio (speakers/headphones) and convert to text in real-time
3. **AI-powered summaries** — generate structured meeting overviews and action items using a local LLM
4. **Flexible destinations** — save notes to multiple integration providers: Notion databases (primary), Google Drive, OneDrive/SharePoint, Confluence, Slack, or export locally as CSV, Excel, Markdown, or PDF. Users choose where their notes go.
5. **Privacy-first** — all processing happens locally by default (Private Mode via LLamaSharp). Optional cloud API mode available for users who prefer faster/higher-quality summaries — requires explicit opt-in and uses the user's own API key (BYOK). Audio never leaves the device regardless of mode.

## Target Users
People who:
- Attend frequent meetings (standups, 1:1s, team syncs, client calls)
- Use Notion, Google Drive, SharePoint, Confluence, or local files as their knowledge base
- Want automated transcription without uploading audio to the cloud
- Need structured meeting records with action items and summaries
- Use Microsoft Teams, Zoom, Google Meet, or Discord for calls

## Current Scope (v0.1–v0.3)
- Main interface with integration selection and recent notes
- Note-taking window with all core sections (transcription, notes, summary, key points, action items)
- System audio capture via WASAPI loopback
- Multi-model ASR via sherpa-onnx (Moonshine + Whisper)
- Speaker diarization + speaker identification (manual, LLM-based, voice fingerprinting)
- AI summaries via LLamaSharp (Private Mode, local) or cloud API (API Key Mode, opt-in)
- Multi-provider integration architecture: Notion (functional), with coming-soon support for Google Drive, OneDrive, Confluence, Slack, CSV, Excel, Markdown, PDF, Webhook
- Settings: sidebar navigation with Integrations, Speech & Audio, AI, and General pages
- Notion API: save notes, fetch recent notes, workspace CRUD

## Future Scope
- Real call detection (monitor processes for active calls)
- System tray operation (background monitoring)
- Auto-save during meetings
- Desktop notifications for detected calls
- Meeting templates (standup, 1:1, retrospective)
- Google Drive, OneDrive/SharePoint, Confluence, Slack integrations
- CSV, Excel, Markdown, PDF local export providers
- Webhook integration for custom API endpoints

## Non-Goals
- Cloud-based transcription (privacy boundary)
- Video recording or screen capture
- Calendar integration or meeting scheduling
- Real-time collaboration or shared editing
- Mobile or cross-platform (Windows desktop only for v1)

## Success Metrics
- User can go from "Start Notes" to saved notes in under 2 minutes (any integration provider)
- Transcription captures spoken content accurately (sherpa-onnx Moonshine/Whisper ASR)
- AI summary produces actionable meeting overviews with extracted action items
- Adding a new integration takes under 60 seconds (zero-friction setup flow)
- In Private Mode: zero data leaves the user's machine (except saves to user's chosen integration)
- In API Key Mode: only transcript text (not audio) is sent to the user's chosen cloud provider
