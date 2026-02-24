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
dotnet restore        # Restore NuGet packages (NAudio, System.Speech)
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
| System.Speech | 8.0.0 | Windows speech recognition engine |
| LLamaSharp | 0.26.0 | In-process LLM inference (Private Mode) |
| LLamaSharp.Backend.Cpu | 0.26.0 | CPU inference backend |
| LLamaSharp.Backend.Cuda12 | 0.26.0 | NVIDIA GPU inference backend (optional) |
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
- Windows Speech Recognition processes the audio after you stop recording (batch mode, not streaming)

## File Locations

| File | Location |
|------|----------|
| Source code | Project root |
| Workspace config | `%AppData%/MeetingNotesApp/workspaces.json` |
| App settings | `%AppData%/MeetingNotesApp/appsettings.json` |
| AI model (Private Mode) | `%AppData%/MeetingNotesApp/models/Phi-4-mini-instruct-Q4_K_M.gguf` |
| Temp audio files | `%TEMP%/*.wav` (auto-cleaned after processing) |

## Known Limitations (v0.1)

- Call detection toggles are UI-only — no actual process monitoring yet
- Speech recognition is Windows built-in (decent accuracy, not Whisper-level)
- Transcription is batch (record → stop → process), not real-time streaming
- Notion rich_text properties have a 2000-character limit per block
- Private Mode requires a one-time 2.49 GB model download on first use
- Private Mode on CPU takes ~15-30 seconds per summary (GPU is ~3 seconds)
- General settings (start minimized, auto-save, notifications) are UI-only — not yet functional
