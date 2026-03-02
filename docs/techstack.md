# techstack.md

## Stack Goals
- Local-first processing (transcription + AI on-device)
- Native Windows audio capture (system audio loopback)
- Multi-provider integration architecture (Notion, Google Drive, CSV, Excel, and more)
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

### Speech Recognition (Multi-Model)
- **sherpa-onnx OfflineRecognizer** — offline neural ASR engine supporting multiple model families
- Uses the **same `org.k2fsa.sherpa.onnx` NuGet package** already installed for speaker diarization — no additional dependencies
- **User-selectable models** (downloaded independently via Settings → Speech Recognition):

| Model | Size | WER | Speed | Config |
|-------|------|-----|-------|--------|
| Moonshine Tiny int8 | ~125 MB (5 files) | ~12% | Very fast | `ModelConfig.Moonshine` |
| Moonshine Base int8 | ~288 MB (5 files) | ~9% | Fast | `ModelConfig.Moonshine` |
| Whisper tiny.en int8 | ~104 MB (3 files) | ~12% | Fast | `ModelConfig.Whisper` |
| Whisper base.en int8 | ~161 MB (3 files) | ~10% | Moderate | `ModelConfig.Whisper` |
| Whisper small.en int8 | ~375 MB (3 files) | ~5% | Slower | `ModelConfig.Whisper` |

- Models stored at `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/{model-folder}/`
- Selected model persisted in `appsettings.json` (ASR.SelectedModel)
- Processes audio as 16kHz mono float[] — handles variable-length segments well
- Flow: Record → Stop → Convert to 16kHz mono WAV → sherpa-onnx diarization → float[] sub-array per segment → sherpa-onnx ASR per segment
- No audio file slicing: full WAV loaded as float[] once, segments accessed by timestamp index into the array
- **Privacy: all processing is local. No network calls. Models run via ONNX Runtime on CPU.**

### Speaker Diarization
- **sherpa-onnx** (`org.k2fsa.sherpa.onnx` v1.12.26) — offline speaker diarization using ONNX Runtime
- Identifies "who spoke when" from a single mixed audio channel (WASAPI loopback output)
- Requires `AllowUnsafeBlocks=true` in csproj (native P/Invoke interop with sherpa-onnx C API)
- Two ONNX models (~36 MB total, downloaded to `%LocalAppData%/MeetingNotesApp/models/sherpa-onnx/`):
  - **Segmentation model**: pyannote-segmentation-3.0 (~6 MB) — detects speech segments and speaker turns in audio
  - **Speaker embedding model**: 3D-Speaker CampPlus (~30 MB) — generates voice fingerprints per segment for clustering
- Audio format: 16kHz mono float[] normalized [-1, 1] (matches existing audio conversion pipeline)
- Processing: **offline only** — requires complete audio buffer, runs after user clicks Stop Recording
- Pipeline: VAD → Speaker Segmentation → Embedding Extraction → Spectral Clustering → speaker-labeled time segments
- Auto-detects number of speakers (configurable override via NumClusters setting)
- Progress callback available during processing (reports chunk-by-chunk progress to UI)
- Output: `OfflineSpeakerDiarizationSegment[]` — each segment has Start (seconds), End (seconds), Speaker (0-indexed int)
- **Privacy: all processing is local. No network calls. Models run via ONNX Runtime on CPU or CUDA GPU.**

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

### Integration Save Architecture

The app uses a multi-provider integration system to save meeting notes to the user's chosen destination.

#### Save Service Interface
```
IMeetingSaveService
├── SaveMeetingAsync(MeetingData, Integration): Task

Implementations:
├── NotionSaveService     — saves to Notion database via REST API
├── CsvSaveService        — exports to .csv files locally (future)
├── ExcelSaveService      — exports to .xlsx files locally (future)
├── MarkdownSaveService   — exports to .md files locally (future)
├── PdfSaveService        — exports to .pdf files locally (future)
├── GoogleDriveSaveService— saves to Google Drive (future)
├── OneDriveSaveService   — saves to OneDrive/SharePoint (future)
├── ConfluenceSaveService — saves to Confluence pages (future)
├── SlackSaveService      — posts to Slack channels (future)
└── WebhookSaveService    — sends to custom API endpoint (future)
```

#### Integration Data Model
```
Integration (abstract base)
├── NotionIntegration      — API key, selected database, databases list
├── CsvExportIntegration   — export folder path (future)
├── ExcelExportIntegration — export path, append mode (future)
└── (more per provider)
```

### Notion Integration
- **Notion REST API v2022-06-28** — Direct HTTP calls via `HttpClient`
- Database query: `POST /v1/databases/{id}/query` (fetch recent notes)
- Page creation: `POST /v1/pages` (save meeting notes)
- Database search: `POST /v1/search` (discover databases during workspace setup)
- Properties mapped: Title, Transcription, My Notes, AI Summary, Key Points, Action Items, Duration, Organizer, Attendees, Date

### State + Storage
- **Integration config**: `%AppData%/MeetingNotesApp/integrations.json` (JSON, all integration providers — replaces `workspaces.json`)
- **App settings**: `%AppData%/MeetingNotesApp/appsettings.json` (JSON, AI mode, cloud API key, model path)
- **Speaker profiles**: `%AppData%/MeetingNotesApp/speaker_profiles.json` (voice fingerprints for cross-meeting identification)
- **AI model**: `%LocalAppData%/MeetingNotesApp/models/Phi-4-mini-instruct-Q4_K_M.gguf` (downloaded on first use)
- **Meeting data**: Saved to user's chosen integration provider (not stored locally beyond the session)

### Future NuGet Dependencies (for additional providers)
| Package | Purpose |
|---------|---------|
| ClosedXML | Excel (.xlsx) export — MIT license, ~2MB |
| Google.Apis.Drive.v3 | Google Drive integration |
| itext7 or QuestPDF | PDF export |

## Architecture
- **Code-behind pattern** — each window has `.xaml` + `.xaml.cs` with direct event handlers
- **INotifyPropertyChanged** on all window classes for data binding
- **No dependency injection** — services instantiated inline
- **HttpClient** created per-use (consider `IHttpClientFactory` in future)

## NuGet Packages
| Package | Version | Purpose |
|---------|---------|---------|
| NAudio | 2.2.1 | System audio capture (WASAPI loopback) |
| LLamaSharp | 0.26.0 | In-process LLM inference (.NET binding for llama.cpp) |
| LLamaSharp.Backend.Cpu | 0.26.0 | CPU inference backend (works on all machines) |
| LLamaSharp.Backend.Cuda12 | 0.26.0 | NVIDIA GPU inference backend (optional, for CUDA 12.x) |
| org.k2fsa.sherpa.onnx | 1.12.26 | Offline speaker diarization + multi-model ASR: Moonshine + Whisper (ONNX Runtime) |
| Microsoft.Extensions.Logging.Debug | 8.0.0 | Debug logging |
