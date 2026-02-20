# techstack.md

## Stack Goals
- Local-first processing (transcription + AI on-device)
- Native Windows audio capture (system audio loopback)
- Direct Notion API integration (no middleware)
- Fast startup, responsive UI during recording

## Stack

### Language / Framework
- **C# + .NET 8** — Runtime
- **WPF** — UI Framework (XAML + code-behind)
- **INotifyPropertyChanged** — Data binding pattern (not full MVVM)

### Audio Capture
- **NAudio 2.2.1** — `WasapiLoopbackCapture` for system audio (speakers/headphones output)
- Captures what the user hears during a meeting, not microphone input
- Audio format: system default (typically 32-bit float, 48kHz, stereo)
- Converted to 16kHz mono WAV for speech recognition compatibility

### Speech Recognition
- **System.Speech 8.0** — Windows built-in speech recognition engine
- `SpeechRecognitionEngine` with `DictationGrammar` for free-form speech
- Processes captured audio files (not real-time streaming)
- Flow: Record → Stop → Convert to 16kHz mono WAV → Feed to SpeechRecognitionEngine → Append result

### AI Summarization
- **LMStudio** — Local LLM server running on `http://127.0.0.1:1234`
- Model: `meta-llama-3.1-8b-instruct`
- OpenAI-compatible `/v1/chat/completions` endpoint
- Structured prompt for meeting overview + action items extraction
- Fallback: simple text truncation if LMStudio is unavailable

### Notion Integration
- **Notion REST API v2022-06-28** — Direct HTTP calls via `HttpClient`
- Database query: `POST /v1/databases/{id}/query` (fetch recent notes)
- Page creation: `POST /v1/pages` (save meeting notes)
- Database search: `POST /v1/search` (discover databases during workspace setup)
- Properties mapped: Title, Transcription, My Notes, AI Summary, Key Points, Action Items, Duration, Organizer, Attendees, Date

### State + Storage
- **Workspace config**: `%AppData%/MeetingNotesApp/workspaces.json` (JSON, contains API keys)
- **App settings**: `%AppData%/MeetingNotesApp/appsettings.json` (JSON)
- **Meeting data**: Notion databases only (no local persistence of notes)

## Architecture
- **Code-behind pattern** — each window has `.xaml` + `.xaml.cs` with direct event handlers
- **INotifyPropertyChanged** on all window classes for data binding
- **No dependency injection** — services instantiated inline
- **HttpClient** created per-use (consider `IHttpClientFactory` in future)

## NuGet Packages
| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1 | System audio capture (WASAPI loopback) |
| System.Speech | 8.0.0 | Windows speech recognition |
| Microsoft.Extensions.Logging.Debug | 8.0.0 | Debug logging |
