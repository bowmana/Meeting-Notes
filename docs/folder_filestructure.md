# Meeting Notes App Codebase File Structure

```
meeting notes/
├── CLAUDE.md                              # AI agent instructions
├── README.md                              # Project overview and setup instructions
├── MeetingNotesApp.csproj                 # Project file (.NET 8, WPF, NAudio + sherpa-onnx + LLamaSharp NuGet)
├── App.xaml                               # Application entry, global resource dictionaries
├── App.xaml.cs                            # Startup logic: registers CrashLogService global exception handlers
│
├── MainWindow.xaml                        # Main interface: status bar, integration selector dropdown, test functions, recent notes
├── MainWindow.xaml.cs                     # Code-behind: INotifyPropertyChanged, Notion recent notes fetch, integration selector (replaces database dropdown), dynamic connection status
│                                          #   Uses Integration/NotionIntegration models (LoadIntegrations). Classes: MainWindow, DetectedApp, RecentNote
│
├── NoteTakingWindow.xaml                  # Note-taking UI: transcription, notes, summary, key points, action items, speaker identification panel, dynamic save button
├── NoteTakingWindow.xaml.cs               # Code-behind: audio capture (NAudio WASAPI), speech recognition (ISpeechRecognitionService / sherpa-onnx Moonshine ASR),
│                                          #   AI summary (via IAiSummaryService), speaker identification pipeline (voice fingerprinting + LLM inference),
│                                          #   dynamic SaveButtonText from Integration.SaveButtonText, Notion save, duration timer
│                                          #   Classes: NoteTakingWindow, KeyPoint, ActionItem, SpeakerEntry
│
├── SettingsWindow.xaml                    # Settings UI: sidebar navigation (Integrations, Speech & Audio, AI, General), provider picker, integration CRUD
├── SettingsWindow.xaml.cs                 # Code-behind: sidebar navigation (SelectedSettingsPage), integration CRUD using NotionIntegration, Notion API database fetch,
│                                          #   diarization model download, multi-model ASR download/delete/selection, integration persistence (JSON), settings save/load
│                                          #   Static methods: LoadIntegrations/SaveIntegrations (with auto-migration from workspaces.json)
│                                          #   Classes: SettingsWindow, EqualityConverter (IMultiValueConverter), ASRModelViewModel, AppSettings (static), DiarizationSettings, ASRSettings
│
├── TranscriptionSegment.cs               # Data models for speaker-tagged transcription
│                                          #   Classes: TranscriptionSegment (+ CustomSpeakerName, EffectiveSpeakerName),
│                                          #            DiarizedTranscription (+ SpeakerNames dict, SetSpeakerName, GetNamedAttendees, GetSpeakerIndices)
│
├── SpeakerNameInferenceService.cs        # LLM-based speaker name inference from transcript context
│                                          #   Uses LLamaSharp Phi-4-mini to infer names from conversational cues
│                                          #   Classes: SpeakerNameInference, ISpeakerNameInferenceService, SpeakerNameInferenceService
│
├── SpeakerEmbeddingHelper.cs             # Speaker voice embedding extraction via sherpa-onnx
│                                          #   Wraps SpeakerEmbeddingExtractor with 3D-Speaker CampPlus model
│                                          #   Class: SpeakerEmbeddingHelper
│
├── SpeakerProfileService.cs              # Cross-meeting speaker voice profile persistence
│                                          #   Stores voice fingerprints at %AppData%/speaker_profiles.json
│                                          #   Cosine similarity matching against enrolled profiles
│                                          #   Classes: SpeakerProfile, SpeakerProfileService
│
├── ISpeakerDiarizationService.cs         # Interface for speaker diarization service
│                                          #   Interface: ISpeakerDiarizationService (AreModelsAvailable, DiarizeAsync)
│
├── SherpaOnnxDiarizationService.cs       # sherpa-onnx implementation of speaker diarization
│                                          #   Uses OfflineSpeakerDiarization with pyannote segmentation + 3D-Speaker embeddings
│                                          #   Class: SherpaOnnxDiarizationService
│
├── AudioHelper.cs                        # Shared audio utility (LoadWavAsFloats)
│                                          #   Loads WAV file as 16kHz mono float[] for sherpa-onnx consumption
│                                          #   Class: AudioHelper
│
├── ASRModelDefinition.cs                # ASR model registry (enum + metadata for all available models)
│                                          #   Enum: ASRModelType (MoonshineTiny, MoonshineBase, WhisperTinyEn, WhisperBaseEn, WhisperSmallEn)
│                                          #   Class: ASRModelDefinition (DisplayName, FolderName, IsMoonshine, Files[], download URLs)
│
├── ASRModelManager.cs                    # Multi-model ASR download and path management
│                                          #   Per-model download, delete, path resolution, existence checks
│                                          #   Supports Moonshine (5 files) and Whisper (3 files) model families
│                                          #   Class: ASRModelManager
│
├── ISpeechRecognitionService.cs          # Interface for speech recognition service
│                                          #   Interface: ISpeechRecognitionService (AreModelsAvailable, CurrentModelType, SetModel, TranscribeAsync)
│
├── SherpaOnnxASRService.cs               # sherpa-onnx multi-model OfflineRecognizer implementation
│                                          #   Lazy-cached OfflineRecognizer, Moonshine vs Whisper config branching
│                                          #   Class: SherpaOnnxASRService
│
├── CrashLogService.cs                   # Global crash logging (AppDomain + WPF Dispatcher + TaskScheduler)
│                                          #   Writes crash reports to %AppData%/MeetingNotesApp/crashlog.txt
│                                          #   Class: CrashLogService
│
├── CallDetectionService.cs               # Meeting platform call detection (process monitoring + audio state)
│                                          #   Class: CallDetectionService
│
├── DiarizationModelDefinition.cs         # Diarization segmentation model registry (enum + metadata)
│                                          #   Enum: DiarizationSegmentationModelType (Pyannote3, ReverbV1)
│                                          #   Class: DiarizationModelDefinition (DisplayName, FolderName, Url, ExpectedSizeBytes)
│
├── DiarizationModelManager.cs            # Diarization model download and path management
│                                          #   Per-model segmentation download/delete/paths + single embedding model
│                                          #   Class: DiarizationModelManager
│
├── LLamaSharpDebugWindow.xaml            # Debug panel: model download banner, model loading, inference testing, streaming output
├── LLamaSharpDebugWindow.xaml.cs         # Code-behind: model detection + download from Hugging Face with progress,
│                                          #   LLamaWeights model loading, StatelessExecutor inference,
│                                          #   DefaultSamplingPipeline config, async streaming tokens, resource disposal
│
├── Models/                                # Data model classes (v0.3)
│   ├── Integration.cs                     # Abstract base class + IntegrationProviderType enum (Id, DisplayName, ProviderType, StatusText, StatusColor, ProviderDisplayName, TargetDescription, SaveButtonText)
│   ├── NotionIntegration.cs               # Notion-specific integration (ApiKey, SelectedDatabase, Databases) — replaces NotionWorkspaceIntegration
│   ├── NotionDatabase.cs                  # Notion database model (Name, Id, Type) — moved from SettingsWindow.xaml.cs in Phase D
│   ├── CsvExportIntegration.cs            # CSV export integration (ExportFolderPath)
│   ├── ExcelExportIntegration.cs          # Excel export integration (ExportPath, AppendToSingleFile)
│   ├── SerializableIntegration.cs         # JSON-safe flat model with ProviderType discriminator — replaces SerializableWorkspace
│   └── MeetingInfo.cs                     # Meeting session data (Title, Organizer, Attendees, Integration, StartTime) — moved from MeetingSetupWindow.xaml.cs
│
├── Services/                              # Business logic services (v0.3)
│   ├── IMeetingSaveService.cs             # IMeetingSaveService interface + MeetingData DTO (provider-agnostic meeting data container)
│   ├── NotionSaveService.cs               # Notion API save implementation (extracted from NoteTakingWindow — property mapping, 2000-char truncation, speaker data)
│   ├── CsvSaveService.cs                  # CSV file export placeholder (throws NotImplementedException — coming soon)
│   └── ExcelSaveService.cs                # Excel file export placeholder (throws NotImplementedException — coming soon)
│
├── Styles/                                # XAML resource dictionaries
│   ├── Colors.xaml                        # Color palette: primary (grey-green), backgrounds (dark), text, status colors
│   └── Styles.xaml                        # Shared control styles (buttons, text, containers)
│
├── img/                                   # Application images
│   ├── meetingnotes.ico                   # Application icon
│   └── meetingnotes_noback.png            # Logo without background (110KB)
│
├── docs/                                  # Project documentation
│   ├── README.md                          # Project overview for docs folder
│   ├── goals.md                           # Product goals, scope, non-goals, success metrics
│   ├── techstack.md                       # Tech stack decisions (.NET 8, WPF, NAudio, sherpa-onnx, LLamaSharp, Notion API)
│   ├── features.md                        # Feature specifications (all windows and interactions)
│   ├── datamodels.md                      # Data models, classes, storage schema, Notion database schema
│   ├── ui.md                              # UI design, color system, window layouts, user flows
│   ├── niche.md                           # Target niche and pain points
│   ├── marketing.md                       # Messaging, positioning, differentiation, FAQ
│   ├── roadmap.md                         # Version roadmap (v0.1 through future)
│   ├── checklist.md                       # Development progress tracker (living document)
│   ├── lessons.md                         # Lessons learned — bugs, failed approaches, patterns to avoid
│   ├── dev-workflow.md                    # Build instructions, prerequisites, development workflow
│   ├── folder_filestructure.md            # This file
│   └── llamasharp/                        # LLamaSharp reference documentation
│       ├── README.md                      # Documentation index
│       ├── overview.md                    # LLamaSharp overview and features
│       ├── gettingstarted.md              # Installation and first run
│       ├── architecture.md                # Architecture (LLamaModel → Executors → ChatSession)
│       ├── basicusage.md                  # ChatSession basic usage
│       ├── ModelParameters.md             # ModelParams reference (ContextSize, GpuLayerCount, etc.)
│       ├── inferenceparameters.md         # InferenceParams reference (MaxTokens, Temperature, etc.)
│       ├── text-to-text-apis.md           # ILLamaExecutor interface (Infer, InferAsync)
│       ├── differencesofexecutors.md      # Interactive vs Instruct vs Stateless executors
│       ├── statelessexecutor.md           # StatelessExecutor example (our primary executor)
│       ├── quantization.md                # Model quantization
│       ├── logger.md                      # Custom ILLamaLogger for WPF
│       ├── transforms.md                  # Input/output/history transforms
│       └── apireference.md                # Full API reference index
│
├── bin/                                   # Build output (git-ignored)
└── obj/                                   # Build intermediates (git-ignored)
```

## Storage Locations (Runtime)

```
%AppData%/MeetingNotesApp/
├── integrations.json        # All configured integrations — Notion, CSV, Excel, etc. (replaces workspaces.json)
├── workspaces.json          # Legacy — auto-migrated to integrations.json on first load
├── appsettings.json         # App-level settings (AI mode, cloud API key, diarization, ASR model selection)
├── speaker_profiles.json    # Enrolled speaker voice profiles (name + embedding vector + metadata)
└── crashlog.txt             # Crash reports (unhandled exceptions with stack traces, appended per crash)

%LocalAppData%/MeetingNotesApp/
└── models/
    ├── Phi-4-mini-instruct-Q4_K_M.gguf                          # Downloaded LLM model for Private Mode (~2.49 GB)
    └── sherpa-onnx/                                              # sherpa-onnx models (diarization + ASR)
        ├── sherpa-onnx-pyannote-segmentation-3-0/model.onnx      # Diarization segmentation model (~6 MB)
        ├── 3dspeaker_speech_campplus_sv_en_voxceleb_16k.onnx     # Diarization speaker embedding model (~30 MB)
        ├── moonshine-tiny-int8/                                   # Moonshine Tiny ASR (~125 MB, 5 files)
        ├── moonshine-base-int8/                                   # Moonshine Base ASR (~288 MB, 5 files)
        ├── whisper-tiny-en-int8/                                  # Whisper tiny.en ASR (~104 MB, 3 files)
        ├── whisper-base-en-int8/                                  # Whisper base.en ASR (~161 MB, 3 files)
        └── whisper-small-en-int8/                                 # Whisper small.en ASR (~375 MB, 3 files)
    # Note: Uses LocalAppData (not Roaming AppData) to avoid syncing large models on corporate networks
    # Note: Only models the user downloads are present; all 5 are optional
```

## Tech Stack

- **.NET 8** — Runtime
- **WPF** — UI Framework
- **NAudio 2.2.1** — System audio capture (WASAPI loopback)
- **sherpa-onnx Moonshine Tiny ASR** — Offline neural speech recognition (via org.k2fsa.sherpa.onnx)
- **LLamaSharp 0.26.0** — In-process LLM inference for Private Mode (+ Backend.Cpu / Backend.Cuda12)
- **org.k2fsa.sherpa.onnx 1.12.26** — Offline speaker diarization (pyannote segmentation + 3D-Speaker embeddings)
- **Notion REST API** — Note storage and retrieval

## Architecture Overview

| Layer | Description |
|-------|-------------|
| **Windows** | WPF windows with code-behind + INotifyPropertyChanged (MainWindow, NoteTakingWindow, SettingsWindow, LLamaSharpDebugWindow) |
| **Models** | `Models/` directory: Integration (base), NotionIntegration, CsvExportIntegration, ExcelExportIntegration, SerializableIntegration, NotionDatabase, MeetingInfo. Inline: DetectedApp, RecentNote, KeyPoint, ActionItem |
| **Services** | `Services/` directory: IMeetingSaveService (interface + MeetingData DTO), NotionSaveService, CsvSaveService, ExcelSaveService |
| **APIs** | Notion REST API (HttpClient), Cloud AI provider OpenAI-compatible API (HttpClient, API Key Mode only) |
| **Audio** | NAudio WasapiLoopbackCapture → WAV conversion → sherpa-onnx speaker diarization → sherpa-onnx Moonshine ASR per-segment transcription (float[] sub-arrays) |
| **Persistence** | JSON files in %AppData%/MeetingNotesApp/ for integration config, app settings, speaker profiles |

## Key Dependencies

| Class/Window | Key Dependencies |
|-------------|-----------------|
| MainWindow | Integration (base), NotionIntegration, RecentNote, HttpClient (Notion API), LLamaSharpDebugWindow |
| NoteTakingWindow | MeetingInfo, Integration, IMeetingSaveService (NotionSaveService), MeetingData, NAudio (WasapiLoopbackCapture, AudioFileReader, MediaFoundationResampler), ISpeechRecognitionService (SherpaOnnxASRService), ISpeakerDiarizationService, AudioHelper, DiarizedTranscription |
| SettingsWindow | Integration (base), NotionIntegration, NotionDatabase, SerializableIntegration, AppSettings, AiSettings, HttpClient (Notion API) |
