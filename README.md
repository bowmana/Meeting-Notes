# Meeting Notes App

A Windows 11 application that automatically detects calls and helps you take notes, with integration to Notion.

## Features

### ✅ Implemented (Basic UI)
- **Main Interface**: Clean, modern Windows 11-style UI
- **Call Detection Settings**: Configure which apps to monitor (Teams, Zoom, Meet, Discord)
- **Notion Integration**: API key input and database selection
- **Status Monitoring**: Real-time status of call detection
- **Note-Taking Window**: Full-featured note-taking interface with:
  - Live transcription display
  - Manual notes editor
  - Key points tracking
  - Action items management
  - Meeting information display

### 🔄 To Be Implemented
- **Real Call Detection**: Monitor system audio and processes
- **Speech-to-Text**: OpenAI Whisper integration
- **Notion API**: Save notes to selected database
- **AI Summarization**: Generate meeting summaries
- **System Tray**: Background operation

## How It Works

1. **Call Detection**: The app monitors for active calls in Teams, Zoom, Google Meet
2. **Popup Notification**: When a call is detected, a popup asks if you want to take notes
3. **Note Taking**: If you choose "Yes", the note-taking window opens with:
   - Live transcription of the meeting
   - Ability to add manual notes
   - Track key points and action items
4. **Save to Notion**: Notes are automatically saved to your selected Notion database

## Setup Instructions

### Prerequisites
1. **Install .NET 8 SDK**: Download from https://dotnet.microsoft.com/download
2. **Visual Studio 2022** or **Visual Studio Code** with C# extension
3. **Notion API Key**: Create an integration at https://www.notion.so/my-integrations
4. **OpenAI API Key**: For transcription and summarization

### Running the App
1. Open terminal in the project directory
2. Run: `dotnet restore` (to restore packages)
3. Run: `dotnet build` (to build the project)
4. Run: `dotnet run` (to run the application)

### Configuration
1. Enter your Notion API key in the main interface
2. Select which Notion database to use
3. Enable call detection for your preferred apps
4. The app will run in the background and detect calls

## UI Preview

### Main Interface
- **Status Section**: Shows current monitoring status
- **Notion Database Selection**: Configure API key and select database
- **Call Detection Settings**: Enable/disable monitoring for different apps
- **Test Functions**: Simulate call detection and test connections
- **Recent Notes**: View your recent meeting notes

### Note-Taking Window
- **Meeting Information**: Platform, start time, duration
- **Live Transcription**: Real-time speech-to-text
- **Manual Notes**: Add your own notes
- **Key Points**: Track important discussion points
- **Action Items**: Manage tasks and assignments
- **Actions**: Stop recording, save to Notion, generate AI summary

## Next Steps

The basic UI framework is complete! Next we'll implement:
1. Real call detection using Windows audio APIs
2. OpenAI Whisper integration for transcription
3. Notion API integration for saving notes
4. AI summarization using GPT-4
5. System tray functionality for background operation

## Technical Details

- **Framework**: .NET MAUI (Multi-platform App UI)
- **Target**: Windows 11 (Windows 10.0.17763.0+)
- **UI**: XAML with data binding
- **Architecture**: MVVM pattern with INotifyPropertyChanged
- **Styling**: Windows 11 Fluent Design principles
