# Meeting Notes App Development Workflow

## Prerequisites

1. **.NET 8 SDK** — Download from https://dotnet.microsoft.com/download
2. **Visual Studio 2022** or **Visual Studio Code** with C# extension
3. **LMStudio** — Download from https://lmstudio.ai
   - Load `meta-llama-3.1-8b-instruct` model
   - Start local server on `http://127.0.0.1:1234`
4. **Notion API Key** — Create an integration at https://www.notion.so/my-integrations
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

## Testing LMStudio Connection

1. Ensure LMStudio is running with `meta-llama-3.1-8b-instruct` loaded
2. Open Settings → AI Settings section
3. Click "Test LMStudio Connection"
4. A dialog will show success or failure

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
| Temp audio files | `%TEMP%/*.wav` (auto-cleaned after processing) |

## Known Limitations (v0.1)

- Call detection toggles are UI-only — no actual process monitoring yet
- Speech recognition is Windows built-in (decent accuracy, not Whisper-level)
- Transcription is batch (record → stop → process), not real-time streaming
- Notion rich_text properties have a 2000-character limit per block
- LMStudio must be running manually before using AI summary
- General settings (start minimized, auto-save, notifications) are UI-only — not yet functional
