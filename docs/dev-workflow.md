# Meeting Notes App Development Workflow

## Prerequisites

1. **.NET 8 SDK** — Download from https://dotnet.microsoft.com/download
2. **Visual Studio 2022** or **Visual Studio Code** with C# extension
3. **Notion API Key** — Create an integration at https://www.notion.so/my-integrations
   - Create a database with "Meetings" in the name
   - Share the database with your integration
   - Required properties: Meeting Title (title), Transcription (rich_text), Your Notes (rich_text), AI Summary (rich_text), Key Points (rich_text), Action Items (rich_text), Duration (rich_text), Organizer (rich_text), Attendees (rich_text), Date (date), Created time (created_time)

## Building and Running

### Command Line
```bash
cd "meeting notes"
dotnet restore        # Restore NuGet packages (NAudio, sherpa-onnx, LLamaSharp)
dotnet build          # Build the project
dotnet run            # Run the application
```

### Visual Studio
1. Open `MeetingNotesApp.csproj` in Visual Studio 2022
2. Press F5 to build and run

## Project Configuration

| Setting | Value |
|---------|-------|
| Target Framework | `net8.0-windows` |
| Output Type | WinExe |
| WPF | Enabled |
| Nullable | Enabled |
| Implicit Usings | Enabled |

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1 | System audio capture (WASAPI loopback) |
| LLamaSharp | 0.26.0 | In-process LLM inference (Private Mode) |
| LLamaSharp.Backend.Cpu | 0.26.0 | CPU inference backend |
| LLamaSharp.Backend.Cuda12 | 0.26.0 | NVIDIA GPU inference backend (optional) |
| org.k2fsa.sherpa.onnx | 1.12.26 | Offline speaker diarization + Moonshine Tiny ASR (sherpa-onnx, ONNX models) |
| Microsoft.Extensions.Logging.Debug | 8.0.0 | Debug logging |

## First-Time Setup

1. Build and run the app
2. Click the gear icon (Settings)
3. Click "Add Workspace"
4. Enter a display name (e.g., "My Workspace")
5. Paste your Notion API key
6. Click "Fetch Databases" — your databases containing "Meetings" will appear
7. Select the target database
8. Click "Save"
9. Close Settings
10. Select the database in the main window dropdown
11. Click "Start Notes" to begin a meeting

## AI Summarization Setup

### Private Mode (Default — Local AI)
1. Open Settings → AI Settings section
2. Ensure "Private Mode" is selected
3. Click "Download Model" if model is not yet downloaded (Phi-4-mini, 2.49 GB from Hugging Face)
4. Wait for download to complete (progress bar shown)
5. Click "Test Local AI" to verify the model works
6. AI summaries will now run entirely on your device

### API Key Mode (Optional — Cloud AI)
1. Open Settings → AI Settings section
2. Select "API Key Mode"
3. Choose a provider (OpenAI, Anthropic, or Custom)
4. Paste your API key
5. Enter the model name (e.g., "gpt-4o-mini")
6. Click "Test Cloud AI" to verify the connection
7. AI summaries will use the cloud provider (transcript text is sent to the provider)

## Audio Capture Notes

- The app captures **system audio output** (WASAPI loopback), not microphone input
- This means it records what you hear through your speakers/headphones
- For meeting transcription, your meeting audio must play through your default audio output device
- Audio is captured as the system default format, then converted to 16kHz mono WAV for speech recognition
- sherpa-onnx Moonshine Tiny ASR processes the audio after you stop recording (batch mode, not streaming)

## File Locations

| File | Location |
|------|----------|
| Source code | Project root |
| Workspace config | `%AppData%/MeetingNotesApp/workspaces.json` |
| App settings | `%AppData%/MeetingNotesApp/appsettings.json` |
| AI model (Private Mode) | `%LocalAppData%/MeetingNotesApp/models/Phi-4-mini-instruct-Q4_K_M.gguf` |
| Diarization models | `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/` (~36 MB) |
| ASR models | `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/moonshine-tiny-int8/` (~125 MB) |
| Temp audio files | `%TEMP%/*.wav` (auto-cleaned after processing) |

## Speaker Diarization Setup

1. Open Settings → Speaker Diarization section
2. Click "Download Models" (~36 MB download from GitHub releases)
3. Wait for download to complete (progress bar shown)
4. Models are stored at `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/`
5. Speaker diarization will now run automatically after each recording stops
6. Optional: adjust number of speakers (default: auto-detect) and clustering threshold in advanced settings

## Speech Recognition Setup

1. Open Settings → Speech Recognition section
2. Click "Download Models" (~125 MB download — Moonshine Tiny int8 ASR model)
3. Wait for download to complete (progress bar shown)
4. Models are stored at `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/moonshine-tiny-int8/` (5 files)
5. ASR will now run automatically on each speaker segment after diarization completes
6. Uses the same `org.k2fsa.sherpa.onnx` NuGet package as diarization — no additional dependencies

## Known Limitations (v0.1 / v0.2)

- Call detection toggles are UI-only — no actual process monitoring yet
- Speech recognition uses Moonshine Tiny int8 (~12% WER) — good accuracy but not state-of-the-art; requires ~125 MB model download
- Transcription is batch (record → stop → process), not real-time streaming
- Notion rich_text properties have a 2000-character limit per block
- Private Mode requires a one-time 2.49 GB model download on first use
- Private Mode on CPU takes ~15-30 seconds per summary (GPU is ~3 seconds)
- General settings (start minimized, auto-save, notifications) are UI-only — not yet functional
- Speaker diarization is post-recording only (not real-time) — processes full audio after Stop Recording
- Diarization models require a one-time ~36 MB download
- Speaker labels are generic ("Speaker 1", "Speaker 2") — no voice recognition or name matching
- Moonshine Tiny handles variable-length segments well, but very short segments (<1 second) may produce less accurate results
- Diarization processing time scales with recording length (~30-120 seconds for a 30-minute meeting on CPU)
