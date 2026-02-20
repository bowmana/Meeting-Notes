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

## AppSettings (static)

```csharp
// Defined in SettingsWindow.xaml.cs
public static class AppSettings
{
    public static void SaveSettings();
    public static void LoadSettings();
}
```

Static utility for persisting app-level settings to `%AppData%/MeetingNotesApp/appsettings.json`. Currently a placeholder — no settings are actively stored beyond workspaces.

---

## Storage Locations (Runtime)

```
%AppData%/MeetingNotesApp/
├── workspaces.json        # Configured Notion workspace integrations (includes API keys)
└── appsettings.json       # App-level settings (currently empty placeholder)
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
