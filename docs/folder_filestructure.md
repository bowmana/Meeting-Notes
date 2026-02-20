# Meeting Notes App Codebase File Structure

```
meeting notes/
├── CLAUDE.md                              # AI agent instructions
├── README.md                              # Project overview and setup instructions
├── MeetingNotesApp.csproj                 # Project file (.NET 8, WPF, NAudio + System.Speech NuGet)
├── App.xaml                               # Application entry, global resource dictionaries
├── App.xaml.cs                            # Startup logic (empty — default WPF startup)
│
├── MainWindow.xaml                        # Main interface: status bar, database selection, test functions, recent notes
├── MainWindow.xaml.cs                     # Code-behind: INotifyPropertyChanged, Notion recent notes fetch, database refresh, navigation
│                                          #   Classes: MainWindow, DetectedApp, RecentNote
│
├── MeetingSetupWindow.xaml                # Meeting setup form: workspace selection, meeting info entry
├── MeetingSetupWindow.xaml.cs             # Code-behind: form validation, creates MeetingInfo, opens NoteTakingWindow
│                                          #   Classes: MeetingSetupWindow, MeetingInfo
│
├── NoteTakingWindow.xaml                  # Note-taking UI: transcription, notes, summary, key points, action items
├── NoteTakingWindow.xaml.cs               # Code-behind: audio capture (NAudio WASAPI), speech recognition (System.Speech),
│                                          #   AI summary (LMStudio), Notion save, duration timer
│                                          #   Classes: NoteTakingWindow, KeyPoint, ActionItem
│
├── SettingsWindow.xaml                    # Settings UI: workspace management, AI config, call detection, general settings
├── SettingsWindow.xaml.cs                 # Code-behind: Notion API workspace CRUD, database fetch, LMStudio test,
│                                          #   workspace persistence (JSON), settings save/load
│                                          #   Classes: SettingsWindow, SerializableWorkspace, NotionWorkspaceIntegration,
│                                          #            NotionDatabase, AppSettings (static)
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
│   ├── techstack.md                       # Tech stack decisions (.NET 8, WPF, NAudio, System.Speech, LMStudio, Notion API)
│   ├── features.md                        # Feature specifications (all windows and interactions)
│   ├── datamodels.md                      # Data models, classes, storage schema, Notion database schema
│   ├── ui.md                              # UI design, color system, window layouts, user flows
│   ├── niche.md                           # Target niche and pain points
│   ├── marketing.md                       # Messaging, positioning, differentiation, FAQ
│   ├── roadmap.md                         # Version roadmap (v0.1 through future)
│   ├── checklist.md                       # Development progress tracker (living document)
│   ├── lessons.md                         # Lessons learned — bugs, failed approaches, patterns to avoid
│   ├── dev-workflow.md                    # Build instructions, prerequisites, development workflow
│   └── folder_filestructure.md            # This file
│
├── bin/                                   # Build output (git-ignored)
└── obj/                                   # Build intermediates (git-ignored)
```

## Storage Locations (Runtime)

```
%AppData%/MeetingNotesApp/
├── workspaces.json        # Configured Notion workspace integrations (API keys, selected databases)
└── appsettings.json       # App-level settings (currently placeholder)
```

## Tech Stack

- **.NET 8** — Runtime
- **WPF** — UI Framework
- **NAudio 2.2.1** — System audio capture (WASAPI loopback)
- **System.Speech 8.0** — Windows speech recognition
- **LMStudio** — Local LLM for AI summaries (meta-llama-3.1-8b-instruct)
- **Notion REST API** — Note storage and retrieval

## Architecture Overview

| Layer | Description |
|-------|-------------|
| **Windows** | WPF windows with code-behind + INotifyPropertyChanged (MainWindow, NoteTakingWindow, SettingsWindow, MeetingSetupWindow) |
| **Models** | Inline classes: MeetingInfo, NotionWorkspaceIntegration, NotionDatabase, DetectedApp, RecentNote, KeyPoint, ActionItem |
| **APIs** | Notion REST API (HttpClient), LMStudio OpenAI-compatible API (HttpClient) |
| **Audio** | NAudio WasapiLoopbackCapture → WAV conversion → System.Speech SpeechRecognitionEngine |
| **Persistence** | JSON files in %AppData%/MeetingNotesApp/ for workspace config |

## Key Dependencies

| Class/Window | Key Dependencies |
|-------------|-----------------|
| MainWindow | NotionWorkspaceIntegration, NotionDatabase, RecentNote, HttpClient (Notion API) |
| NoteTakingWindow | MeetingInfo, NAudio (WasapiLoopbackCapture, AudioFileReader, MediaFoundationResampler), System.Speech (SpeechRecognitionEngine, DictationGrammar), HttpClient (LMStudio + Notion API) |
| SettingsWindow | NotionWorkspaceIntegration, NotionDatabase, SerializableWorkspace, AppSettings, HttpClient (Notion API + LMStudio) |
| MeetingSetupWindow | MeetingInfo, NotionWorkspaceIntegration |
