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

### AI Summarization (Dual Mode)

The app supports two AI summarization modes. The user selects their preferred mode in Settings.

#### Private Mode (Default) — LLamaSharp (Local, In-Process)
- **LLamaSharp** — C#/.NET binding for llama.cpp, runs AI inference directly in the app process
- No external server, no separate install, no user configuration required
- Model: **Phi-4-mini-instruct Q4_K_M** (3.8B params, 2.49 GB GGUF file)
- Downloaded on first use to `%LocalAppData%/MeetingNotesApp/models/` (LocalAppData avoids roaming profile sync on corporate networks)
- Architecture: `LLamaWeights` → `StatelessExecutor` → one-shot summarization (no conversation memory)
- GPU auto-detection: uses CUDA if NVIDIA GPU available, falls back to CPU automatically
- Key parameters: `ContextSize=4096`, `GpuLayerCount=-1` (full GPU offload), `Temperature=0.3`
- Streaming: tokens streamed to UI via `IAsyncEnumerable<string>` from `InferAsync()`
- Performance: ~15-25 tok/sec on CPU (summary in ~15-30s), ~100+ tok/sec on GPU (~3s)
- RAM: ~3.5 GB while model is loaded (lazy loaded, singleton at app level, disposed on exit)
- Long transcripts: chunked summarization when transcript exceeds ~12,000 chars (4096 token context)
- **Privacy: all data stays on device. No network calls.**

#### API Key Mode (Opt-In) — Cloud Provider (BYOK)
- User provides their own API key for a cloud LLM provider
- Supported providers: OpenAI, Anthropic, or any OpenAI-compatible endpoint
- API format: `POST /v1/chat/completions` (same format, different endpoint)
- Faster and higher quality than local inference, but transcript text leaves the device
- Cost: negligible (~$0.001/meeting with GPT-4o mini, ~$0.02 with GPT-4o)
- Both OpenAI and Anthropic explicitly do not train on API data
- Privacy disclosure shown when user selects this mode
- **Privacy: transcript text sent to cloud provider. Audio always stays local.**

### Notion Integration
- **Notion REST API v2022-06-28** — Direct HTTP calls via `HttpClient`
- Database query: `POST /v1/databases/{id}/query` (fetch recent notes)
- Page creation: `POST /v1/pages` (save meeting notes)
- Database search: `POST /v1/search` (discover databases during workspace setup)
- Properties mapped: Title, Transcription, My Notes, AI Summary, Key Points, Action Items, Duration, Organizer, Attendees, Date

### State + Storage
- **Workspace config**: `%AppData%/MeetingNotesApp/workspaces.json` (JSON, contains Notion API keys)
- **App settings**: `%AppData%/MeetingNotesApp/appsettings.json` (JSON, AI mode, cloud API key, model path)
- **AI model**: `%LocalAppData%/MeetingNotesApp/models/Phi-4-mini-instruct-Q4_K_M.gguf` (downloaded on first use)
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
| LLamaSharp | 0.26.0 | In-process LLM inference (.NET binding for llama.cpp) |
| LLamaSharp.Backend.Cpu | 0.26.0 | CPU inference backend (works on all machines) |
| LLamaSharp.Backend.Cuda12 | 0.26.0 | NVIDIA GPU inference backend (optional, for CUDA 12.x) |
| Microsoft.Extensions.Logging.Debug | 8.0.0 | Debug logging |
