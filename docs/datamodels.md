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

## AppSettings (static)

```csharp
// Defined in SettingsWindow.xaml.cs
public static class AppSettings
{
    public static AiSettings Ai { get; set; } = new AiSettings();
    public static void SaveSettings();
    public static void LoadSettings();
}
```

Static utility for persisting app-level settings to `%AppData%/MeetingNotesApp/appsettings.json`. Stores AI provider configuration (mode, API key, model path, GPU preference).

---

## Storage Locations (Runtime)

```
%AppData%/MeetingNotesApp/
├── workspaces.json        # Configured Notion workspace integrations (includes Notion API keys)
├── appsettings.json       # App-level settings (AI mode, cloud API key, model path, GPU preference)
└── models/
    └── Phi-4-mini-instruct-Q4_K_M.gguf   # Downloaded LLM model for Private Mode (~2.49 GB)
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
| Created time | Created time | Auto-set by Notion (used for sorting recent notes) |
