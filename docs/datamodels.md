# Meeting Notes App Data Models

> Update this file as models are built or changed.

---

## MeetingInfo

```csharp
// Defined in MeetingSetupWindow.xaml.cs
public class MeetingInfo
{
    public DateTime Date { get; set; }
    public string Title { get; set; }
    public string Organizer { get; set; }
    public string Attendees { get; set; }
    public string Comments { get; set; }
    public NotionWorkspaceIntegration Workspace { get; set; }
    public DateTime StartTime { get; set; }
}
```

Passed from MainWindow or MeetingSetupWindow to NoteTakingWindow when starting a new meeting session.

---

## NotionWorkspaceIntegration

```csharp
// Defined in SettingsWindow.xaml.cs
public class NotionWorkspaceIntegration
{
    public string WorkspaceName { get; set; }
    public string WorkspaceId { get; set; }
    public string ApiKey { get; set; }
    public NotionDatabase SelectedDatabase { get; set; }
    public ObservableCollection<NotionDatabase> Databases { get; set; }
    public string StatusText { get; set; }
    public Brush StatusColor { get; set; }          // Not serializable — excluded from JSON
}
```

Represents a configured Notion workspace. Brush properties are runtime-only; serialization uses `SerializableWorkspace`.

---

## SerializableWorkspace

```csharp
// Defined in SettingsWindow.xaml.cs
public class SerializableWorkspace
{
    public string WorkspaceName { get; set; }
    public string WorkspaceId { get; set; }
    public string ApiKey { get; set; }
    public NotionDatabase SelectedDatabase { get; set; }
    public List<NotionDatabase> Databases { get; set; }
    public string StatusText { get; set; }
}
```

JSON-safe version of `NotionWorkspaceIntegration` (no `Brush` properties). Used for persistence in `workspaces.json`.

---

## NotionDatabase

```csharp
// Defined in SettingsWindow.xaml.cs
public class NotionDatabase
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Type { get; set; }    // "Database" or "Page"
}
```

Represents a Notion database or page discovered via the Notion Search API. Filtered to items containing "Meetings" in the name.

---

## DetectedApp

```csharp
// Defined in MainWindow.xaml.cs
public class DetectedApp
{
    public string AppName { get; set; }
    public bool IsEnabled { get; set; }
    public string Status { get; set; }
}
```

Represents a meeting platform that can be monitored for active calls (Teams, Zoom, Meet, Discord).

---

## RecentNote

```csharp
// Defined in MainWindow.xaml.cs
public class RecentNote
{
    public string Title { get; set; }
    public string Date { get; set; }
    public string Preview { get; set; }
    public string NotionUrl { get; set; }
    public string NotionPageId { get; set; }
}
```

Represents a recently saved meeting note fetched from Notion. Displayed in the main window's Recent Notes list.

---

## KeyPoint

```csharp
// Defined in NoteTakingWindow.xaml.cs
public class KeyPoint
{
    public string Text { get; set; }
    public bool IsCompleted { get; set; }
}
```

A key discussion point tracked during a meeting.

---

## ActionItem

```csharp
// Defined in NoteTakingWindow.xaml.cs
public class ActionItem
{
    public string Text { get; set; }
    public string Assignee { get; set; }
    public bool IsCompleted { get; set; }
}
```

A task or follow-up action identified during a meeting, with an assignee.

---

## TranscriptionSegment

```csharp
// Defined in TranscriptionSegment.cs
public class TranscriptionSegment
{
    public int SpeakerIndex { get; set; }          // 0-indexed speaker ID from diarization (0, 1, 2...)
    public string SpeakerLabel { get; set; }       // Display label: "Speaker 1", "Speaker 2", etc.
    public string Text { get; set; } = "";         // Transcribed text for this segment
    public float StartSeconds { get; set; }        // Segment start time in seconds
    public float EndSeconds { get; set; }          // Segment end time in seconds
    public string TimestampDisplay { get; }        // Computed: "[0:00 - 0:15]"
    public string DisplayText { get; }             // Computed: "Speaker 1 [0:00 - 0:15]: Hello everyone..."
}
```

A single segment of speaker-tagged transcription produced by the sherpa-onnx diarization pipeline. Each segment represents one continuous speech turn by one speaker. The `SpeakerIndex` comes from sherpa-onnx's clustering output (0-indexed), and `SpeakerLabel` is the user-facing display string.

---

## DiarizedTranscription

```csharp
// Defined in TranscriptionSegment.cs
public class DiarizedTranscription
{
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public int SpeakerCount { get; set; }          // Total distinct speakers detected
    public TimeSpan TotalDuration { get; set; }    // Total audio duration

    public string ToFlatText();    // Speaker-labeled text for Notion: "Speaker 1 [0:00 - 0:15]: text\n..."
    public string ToPlainText();   // Raw text without labels: "text text text..."
}
```

Complete result of a diarization + transcription pipeline run. `ToFlatText()` produces the speaker-labeled string used for display in the transcription area and for saving to Notion. `ToPlainText()` provides raw text without speaker labels for backward compatibility.

---

## DiarizationSegmentationModelType (enum)

```csharp
// Defined in DiarizationModelDefinition.cs
public enum DiarizationSegmentationModelType
{
    Pyannote3,      // pyannote-segmentation-3.0 (baseline)
    ReverbV1        // reverb-diarization-v1 (fine-tuned on 26K hours, ~16% more accurate)
}
```

User-selectable segmentation models for speaker diarization. All models use the same `Pyannote.Model` config path — drop-in replacements.

---

## DiarizationModelDefinition

```csharp
// Defined in DiarizationModelDefinition.cs
public class DiarizationModelDefinition
{
    public DiarizationSegmentationModelType ModelType { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string FolderName { get; }
    public string FileName { get; }
    public string Url { get; }
    public long ExpectedSizeBytes { get; }
    public string SizeText { get; }

    public static readonly DiarizationModelDefinition[] AllModels;
    public static DiarizationModelDefinition Get(DiarizationSegmentationModelType type);
}
```

Static registry of available diarization segmentation models. Used by `DiarizationModelManager` for downloads and `SherpaOnnxDiarizationService` for config.

---

## DiarizationSettings

```csharp
// Defined in SettingsWindow.xaml.cs (inside AppSettings)
public class DiarizationSettings
{
    public int NumSpeakers { get; set; } = -1;         // -1 = auto-detect number of speakers
    public float Threshold { get; set; } = 0.5f;       // Clustering threshold (lower = more speakers, higher = fewer)
    public float MinDurationOn { get; set; } = 0.3f;   // Minimum speech segment duration in seconds
    public float MinDurationOff { get; set; } = 0.5f;  // Minimum silence gap between segments in seconds
    public DiarizationSegmentationModelType SelectedSegmentationModel { get; set; } = DiarizationSegmentationModelType.Pyannote3;
}
```

Persisted to `appsettings.json` alongside `AiSettings`. Controls sherpa-onnx diarization behavior. `NumSpeakers = -1` means the clustering algorithm auto-detects the number of speakers using the `Threshold` value. Setting `NumSpeakers` to a positive integer overrides auto-detection (useful when the user knows how many people are in the meeting). `SelectedSegmentationModel` controls which segmentation model is used for diarization.

---

## AiSettings

```csharp
// Planned for SettingsWindow.xaml.cs or a dedicated Services/ file
public class AiSettings
{
    public AiMode Mode { get; set; } = AiMode.Private;          // Private (LLamaSharp) or CloudApi
    public string CloudProvider { get; set; } = "OpenAI";        // "OpenAI", "Anthropic", "Custom"
    public string CloudApiKey { get; set; } = "";                // User's own API key (BYOK)
    public string CloudModel { get; set; } = "gpt-4o-mini";     // Model name for cloud provider
    public string CloudEndpoint { get; set; } = "";              // Custom endpoint URL (when provider = "Custom")
    public string LocalModelPath { get; set; } = "";             // Path to downloaded GGUF model file
    public bool LocalModelDownloaded { get; set; } = false;      // Whether model has been downloaded
    public bool UseGpu { get; set; } = true;                     // Attempt GPU acceleration (auto-fallback to CPU)
}

public enum AiMode
{
    Private,    // LLamaSharp — local, in-process, no data leaves device
    CloudApi    // Cloud provider — user's API key, transcript sent to cloud
}
```

Persisted to `appsettings.json`. Controls which AI summarization provider is used.

- **Private Mode**: LLamaSharp loads a GGUF model in-process, uses `StatelessExecutor` for one-shot summarization.
- **API Key Mode**: HttpClient calls the user's chosen cloud provider's OpenAI-compatible `/v1/chat/completions` endpoint.

---

## ASRSettings

```csharp
// Defined in SettingsWindow.xaml.cs
public class ASRSettings
{
    public ASRModelType SelectedModel { get; set; } = ASRModelType.MoonshineTiny;
}
```

Persisted to `appsettings.json`. Tracks which ASR model the user has selected for transcription.

---

## ASRModelType (enum)

```csharp
// Defined in ASRModelDefinition.cs
public enum ASRModelType
{
    MoonshineTiny,      // ~125 MB, ~12% WER, fastest
    MoonshineBase,      // ~288 MB, ~9% WER
    WhisperTinyEn,      // ~104 MB, ~12% WER
    WhisperBaseEn,      // ~161 MB, ~10% WER
    WhisperSmallEn      // ~375 MB, ~5% WER, best quality
}
```

All models run via sherpa-onnx OfflineRecognizer. Moonshine models use `ModelConfig.Moonshine`, Whisper models use `ModelConfig.Whisper`.

---

## ASRModelDefinition

```csharp
// Defined in ASRModelDefinition.cs
public class ASRModelDefinition
{
    public ASRModelType ModelType { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string FolderName { get; }
    public bool IsMoonshine { get; }
    public (string fileName, string url, long expectedSize)[] Files { get; }
    public long TotalSizeBytes { get; }
    public string SizeText { get; }

    public static readonly ASRModelDefinition[] AllModels;
    public static ASRModelDefinition Get(ASRModelType type);
}
```

Static registry of all available ASR models. Used by `ASRModelManager` for downloads and `SherpaOnnxASRService` for recognizer config.

---

## AppSettings (static)

```csharp
// Defined in SettingsWindow.xaml.cs
public static class AppSettings
{
    public static AiSettings Ai { get; set; } = new AiSettings();
    public static DiarizationSettings Diarization { get; set; } = new DiarizationSettings();
    public static ASRSettings ASR { get; set; } = new ASRSettings();
    public static void SaveSettings();
    public static void LoadSettings();
}
```

Static utility for persisting app-level settings to `%AppData%/MeetingNotesApp/appsettings.json`. Stores AI provider configuration, diarization settings, and ASR model selection.

---

## Storage Locations (Runtime)

```
%AppData%/MeetingNotesApp/
├── workspaces.json        # Configured Notion workspace integrations (includes Notion API keys)
├── appsettings.json       # App-level settings (AI mode, cloud API key, diarization, ASR model selection)
└── crashlog.txt           # Crash reports with stack traces (appended per crash)

%LocalAppData%/MeetingNotesApp/
└── models/
    ├── Phi-4-mini-instruct-Q4_K_M.gguf                          # LLM model for Private Mode (~2.49 GB)
    └── sherpa-onnx/                                              # sherpa-onnx models (diarization + ASR)
        ├── sherpa-onnx-pyannote-segmentation-3-0/model.onnx      # Diarization segmentation model (~6 MB)
        ├── 3dspeaker_speech_campplus_sv_en_voxceleb_16k.onnx     # Diarization speaker embedding model (~30 MB)
        ├── moonshine-tiny-int8/                                   # Moonshine Tiny ASR (~125 MB, 5 files)
        ├── moonshine-base-int8/                                   # Moonshine Base ASR (~288 MB, 5 files)
        ├── whisper-tiny-en-int8/                                  # Whisper tiny.en ASR (~104 MB, 3 files)
        ├── whisper-base-en-int8/                                  # Whisper base.en ASR (~161 MB, 3 files)
        └── whisper-small-en-int8/                                 # Whisper small.en ASR (~375 MB, 3 files)
```

**Note:** Meeting data (transcriptions, notes, summaries) is NOT stored locally — it is saved directly to Notion via the API.

---

## Notion Database Schema (Expected)

The target Notion database should have these properties for full functionality:

| Property Name | Notion Type | Description |
|--------------|-------------|-------------|
| Meeting Title | Title | Meeting name (required) |
| Transcription | Rich Text | Live transcription text |
| Your Notes / My Notes | Rich Text | Manual notes |
| AI Summary | Rich Text | Generated AI summary |
| Key Points | Rich Text | Bullet list of key points |
| Action Items | Rich Text | Bullet list with assignees |
| Duration | Rich Text | Meeting duration (HH:MM:SS) |
| Organizer | Rich Text | Meeting organizer name |
| Attendees | Rich Text | Comma-separated attendee list |
| Date | Date | Meeting start date/time |
| Speakers | Rich Text | Number of speakers detected (e.g., "3 speakers detected") |
| Created time | Created time | Auto-set by Notion (used for sorting recent notes) |
