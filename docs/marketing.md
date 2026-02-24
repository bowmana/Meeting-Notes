# marketing.md

## Positioning
**The meeting note-taker that respects your privacy.**
Captures audio, transcribes live, generates AI summaries, and saves everything to Notion — all running locally on your machine. No cloud transcription. No data leaves your device.

## Target Keywords
- "meeting notes app Windows"
- "meeting transcription local"
- "notion meeting notes integration"
- "private meeting transcription"
- "AI meeting summary local"
- "meeting notes to notion"
- "offline meeting transcription"

## Value Props

### 1. Local-First Privacy
All audio processing, transcription, and AI summarization happens on your machine. Your meeting audio never touches a cloud server. Unlike Otter.ai, Fireflies, or Microsoft Copilot, Meeting Notes processes everything locally.

### 2. Direct Notion Integration
Notes save directly to your Notion database with structured properties — title, transcription, manual notes, AI summary, key points, action items, organizer, attendees, date, and duration. No copy-pasting. No export/import dance.

### 3. AI-Powered Summaries (Your Choice of Private or Cloud)
Generate structured meeting overviews and action items using built-in local AI (Private Mode via LLamaSharp) — no data leaves your device. Or bring your own API key (OpenAI, Anthropic, etc.) for faster, cloud-powered summaries. You choose the trade-off between privacy and performance.

### 4. Capture System Audio
Records what you hear through your speakers or headphones during a call — works with any meeting platform (Teams, Zoom, Meet, Discord). No need to install bots or browser extensions.

### 5. Structured Notes, Not Just Text
Every meeting is saved with: title, organizer, attendees, transcription, your manual notes, AI summary, key points with checkboxes, and action items with assignees.

## Feature Highlights

1. **Live Transcription** — Capture system audio and convert to text during meetings
2. **AI Summaries** — Generate structured overviews + action items with built-in local AI or your own cloud API key
3. **Notion Integration** — Save directly to your database with full property mapping
4. **Privacy-First** — Local AI by default (Private Mode). Optional cloud API with your own key for faster results.
5. **Multi-Platform Audio** — Works with Teams, Zoom, Meet, Discord — any audio output
6. **Structured Notes** — Key points, action items, organizer, attendees — all organized

## Differentiation (vs Cloud Meeting Assistants)

| Cloud Tools (Otter, Fireflies, etc.) | Meeting Notes App |
|---|---|
| Audio uploaded to cloud servers | 100% local processing |
| Requires account + subscription | No account, no subscription |
| Bot joins your meeting (visible to participants) | Captures system audio silently |
| Notes stored in their platform | Notes saved to YOUR Notion |
| AI runs on their servers | AI runs on your machine by default (Private Mode). Optional cloud API with your own key. |
| Monthly subscription ($10-30/mo) | Free / one-time purchase |
| Dependent on internet connection for transcription | Works offline (except Notion save and optional cloud AI) |

## Trust Builders
- "Your audio never leaves your machine. Ever."
- "Private Mode: AI summaries generated entirely on your device — nothing sent to the cloud."
- "API Key Mode: only transcript text goes to your chosen provider. Audio stays local. Your key, your account."
- "Notes go to YOUR Notion workspace. We don't store anything."
- "No telemetry. No accounts. No subscriptions."

## FAQ Bullets (short)
- Q: Does this record my microphone?
  - A: No — it captures system audio output (what you hear through speakers/headphones). Your mic is not recorded.
- Q: Do I need an internet connection?
  - A: Only to save notes to Notion. Transcription and AI summaries work offline.
- Q: What LLM does it use?
  - A: Private Mode uses Phi-4-mini running locally via LLamaSharp — no internet required. Optionally, bring your own OpenAI or Anthropic API key for cloud-powered summaries (API Key Mode).
- Q: How accurate is the transcription?
  - A: It uses Windows built-in speech recognition. Good for capturing the gist of conversations. Whisper integration is planned for higher accuracy.
- Q: Does it work with [platform]?
  - A: Yes — it captures system audio, so it works with any meeting platform that outputs sound.
