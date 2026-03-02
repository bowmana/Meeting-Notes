# marketing.md

## Positioning
**The meeting note-taker that respects your privacy.**
Captures audio, transcribes live, generates AI summaries, and saves everything to Notion, Google Drive, Excel, or wherever you work — all running locally on your machine. No cloud transcription. No data leaves your device.

## Target Keywords
- "meeting notes app Windows"
- "meeting transcription local"
- "notion meeting notes integration"
- "private meeting transcription"
- "AI meeting summary local"
- "meeting notes to notion"
- "offline meeting transcription"
- "meeting notes to Google Drive"
- "meeting notes CSV export"
- "meeting notes Excel export"
- "private meeting notes app"

## Value Props

### 1. Local-First Privacy
All audio processing, transcription, and AI summarization happens on your machine. Your meeting audio never touches a cloud server. Unlike Otter.ai, Fireflies, or Microsoft Copilot, Meeting Notes processes everything locally.

### 2. Save Anywhere — Notion, Google Drive, Excel, and More
Notes save directly to your Notion database, Google Drive, OneDrive, Confluence, or export locally as CSV, Excel, Markdown, or PDF. Structured properties — title, transcription, manual notes, AI summary, key points, action items, organizer, attendees, date, and duration. No copy-pasting. No export/import dance. Not locked into any single platform.

### 3. AI-Powered Summaries (Your Choice of Private or Cloud)
Generate structured meeting overviews and action items using built-in local AI (Private Mode via LLamaSharp) — no data leaves your device. Or bring your own API key (OpenAI, Anthropic, etc.) for faster, cloud-powered summaries. You choose the trade-off between privacy and performance.

### 4. Capture System Audio
Records what you hear through your speakers or headphones during a call — works with any meeting platform (Teams, Zoom, Meet, Discord). No need to install bots or browser extensions.

### 5. Structured Notes, Not Just Text
Every meeting is saved with: title, organizer, attendees, transcription, your manual notes, AI summary, key points with checkboxes, and action items with assignees.

## Feature Highlights

1. **Live Transcription** — Capture system audio and convert to text during meetings
2. **AI Summaries** — Generate structured overviews + action items with built-in local AI or your own cloud API key
3. **Save Anywhere** — Notion, Google Drive, OneDrive, Confluence, Slack, CSV, Excel, Markdown, PDF, or custom webhook
4. **Privacy-First** — Local AI by default (Private Mode). Optional cloud API with your own key for faster results.
5. **Multi-Platform Audio** — Works with Teams, Zoom, Meet, Discord — any audio output
6. **Structured Notes** — Key points, action items, organizer, attendees — all organized
7. **Speaker Identification** — Automatic speaker labeling with voice fingerprinting across meetings

## Differentiation (vs Cloud Meeting Assistants)

| Cloud Tools (Otter, Fireflies, etc.) | Meeting Notes App |
|---|---|
| Audio uploaded to cloud servers | 100% local processing |
| Requires account + subscription | No account, no subscription |
| Bot joins your meeting (visible to participants) | Captures system audio silently |
| Notes stored in their platform | Notes saved where YOU choose — Notion, Drive, Excel, etc. |
| AI runs on their servers | AI runs on your machine by default (Private Mode). Optional cloud API with your own key. |
| Monthly subscription ($10-30/mo) | Free / one-time purchase |
| Dependent on internet connection for transcription | Works offline (except Notion save and optional cloud AI) |

## Trust Builders
- "Your audio never leaves your machine. Ever."
- "Private Mode: AI summaries generated entirely on your device — nothing sent to the cloud."
- "API Key Mode: only transcript text goes to your chosen provider. Audio stays local. Your key, your account."
- "Notes go where YOU choose — Notion, Google Drive, Excel, or local files. We don't store anything."
- "No telemetry. No accounts. No subscriptions."

## FAQ Bullets (short)
- Q: Does this record my microphone?
  - A: No — it captures system audio output (what you hear through speakers/headphones). Your mic is not recorded.
- Q: Do I need an internet connection?
  - A: Only to save notes to Notion. Transcription and AI summaries work offline.
- Q: What LLM does it use?
  - A: Private Mode uses Phi-4-mini running locally via LLamaSharp — no internet required. Optionally, bring your own OpenAI or Anthropic API key for cloud-powered summaries (API Key Mode).
- Q: Can I use this without Notion?
  - A: Yes — export meeting notes as CSV, Excel, Markdown, or PDF files locally. Or save to Google Drive, OneDrive, Confluence, Slack, or a custom webhook. Notion is just one of many supported destinations.
- Q: How accurate is the transcription?
  - A: It uses sherpa-onnx with Moonshine and Whisper ASR models running locally. Multiple models available with different accuracy/speed trade-offs (5-12% word error rate).
- Q: Does it work with [platform]?
  - A: Yes — it captures system audio, so it works with any meeting platform that outputs sound.
