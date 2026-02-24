using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MeetingNotesApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _statusText = "Ready";
        private string _statusDescription = "Waiting for calls to be detected";
        private Brush _statusColor = Brushes.Gray;
        private bool _isCallDetectionEnabled = true;
        private NotionWorkspaceIntegration _selectedWorkspace;
        private ObservableCollection<NotionWorkspaceIntegration> _availableWorkspaces;
        private ObservableCollection<DetectedApp> _detectedApps;
        private static ObservableCollection<RecentNote> _recentNotes = new ObservableCollection<RecentNote>();
        private NotionDatabase _selectedDatabase;
        private ObservableCollection<NotionDatabase> _availableDatabases;
        private bool _hasDatabases = false;
        private string _connectedWorkspaceSummary = "";
        private bool _isDetectionBannerVisible = false;
        private string _detectionBannerTitle = "";
        private string _detectionBannerMessage = "";
        private Brush _detectionBannerColor = Brushes.Transparent;
        private bool _detectionFoundApps = false;
        private CallDetectionService _callDetectionService;
        private bool _userStartedNotes = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initialize collections
            DetectedApps = new ObservableCollection<DetectedApp>
            {
                new DetectedApp { AppName = "Microsoft Teams", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Zoom", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Google Meet", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Discord", IsEnabled = false, Status = "Disabled" }
            };

            RecentNotes = new ObservableCollection<RecentNote>();

            // Load saved workspaces from persistent storage
            AvailableWorkspaces = SettingsWindow.LoadWorkspaces();
            
            // Load app settings
            AppSettings.LoadSettings();
            
            // Initialize available databases collection
            AvailableDatabases = new ObservableCollection<NotionDatabase>();
            
            // Load recent notes from Notion
            _ = LoadRecentNotesFromNotion(AvailableWorkspaces);

            // Set up list boxes and combo boxes
            RecentNotesListBox.ItemsSource = RecentNotes;
            DatabaseComboBox.ItemsSource = AvailableDatabases;

            // Populate databases from all workspaces
            RefreshDatabaseList();

            // Initialize and start call detection service
            _callDetectionService = new CallDetectionService();
            _callDetectionService.IsEnabled = IsCallDetectionEnabled;
            _callDetectionService.MeetingDetected += OnMeetingDetected;
            _callDetectionService.MeetingEnded += OnMeetingEnded;
            _callDetectionService.DetectionError += OnDetectionError;
            _callDetectionService.Start();
            UpdateStatus();
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string StatusDescription
        {
            get => _statusDescription;
            set
            {
                _statusDescription = value;
                OnPropertyChanged();
            }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsCallDetectionEnabled
        {
            get => _isCallDetectionEnabled;
            set
            {
                _isCallDetectionEnabled = value;
                OnPropertyChanged();
                if (_callDetectionService != null)
                    _callDetectionService.IsEnabled = value;
                UpdateStatus();
            }
        }

        public NotionWorkspaceIntegration SelectedWorkspace
        {
            get => _selectedWorkspace;
            set
            {
                _selectedWorkspace = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NotionWorkspaceIntegration> AvailableWorkspaces
        {
            get => _availableWorkspaces;
            set
            {
                _availableWorkspaces = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DetectedApp> DetectedApps
        {
            get => _detectedApps;
            set
            {
                _detectedApps = value;
                OnPropertyChanged();
            }
        }

        public static ObservableCollection<RecentNote> RecentNotes
        {
            get => _recentNotes;
            set => _recentNotes = value;
        }

        public NotionDatabase SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                _selectedDatabase = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NotionDatabase> AvailableDatabases
        {
            get => _availableDatabases;
            set
            {
                _availableDatabases = value;
                OnPropertyChanged();
            }
        }

        public bool HasDatabases
        {
            get => _hasDatabases;
            set
            {
                _hasDatabases = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDatabasePickerEnabled));
            }
        }

        public bool IsDatabasePickerEnabled => HasDatabases;

        public string ConnectedWorkspaceSummary
        {
            get => _connectedWorkspaceSummary;
            set
            {
                _connectedWorkspaceSummary = value;
                OnPropertyChanged();
            }
        }

        public bool IsDetectionBannerVisible
        {
            get => _isDetectionBannerVisible;
            set
            {
                _isDetectionBannerVisible = value;
                OnPropertyChanged();
            }
        }

        public string DetectionBannerTitle
        {
            get => _detectionBannerTitle;
            set
            {
                _detectionBannerTitle = value;
                OnPropertyChanged();
            }
        }

        public string DetectionBannerMessage
        {
            get => _detectionBannerMessage;
            set
            {
                _detectionBannerMessage = value;
                OnPropertyChanged();
            }
        }

        public Brush DetectionBannerColor
        {
            get => _detectionBannerColor;
            set
            {
                _detectionBannerColor = value;
                OnPropertyChanged();
            }
        }

        public bool DetectionFoundApps
        {
            get => _detectionFoundApps;
            set
            {
                _detectionFoundApps = value;
                OnPropertyChanged();
            }
        }

        private void RefreshDatabaseList()
        {
            // Populate databases from all configured workspaces
            AvailableDatabases.Clear();
            foreach (var workspace in AvailableWorkspaces)
            {
                if (workspace.Databases != null)
                {
                    foreach (var database in workspace.Databases)
                    {
                        // Add workspace name prefix to help identify which workspace the database belongs to
                        var databaseCopy = new NotionDatabase
                        {
                            Name = $"{database.Name} ({workspace.WorkspaceName})",
                            Id = database.Id,
                            Type = database.Type
                        };
                        AvailableDatabases.Add(databaseCopy);
                    }
                }
            }

            // Update connection state
            HasDatabases = AvailableDatabases.Count > 0;

            if (AvailableWorkspaces.Count == 1)
            {
                ConnectedWorkspaceSummary = AvailableWorkspaces[0].WorkspaceName;
            }
            else if (AvailableWorkspaces.Count > 1)
            {
                ConnectedWorkspaceSummary = $"{AvailableWorkspaces.Count} workspaces";
            }
            else
            {
                ConnectedWorkspaceSummary = "";
            }

            // Select first database by default
            if (AvailableDatabases.Count > 0)
            {
                SelectedDatabase = AvailableDatabases[0];
                DatabaseComboBox.SelectedItem = SelectedDatabase;
            }
        }

        private void OnStartNotesClicked(object sender, RoutedEventArgs e)
        {
            if (DatabaseComboBox.SelectedItem == null)
            {
                ValidationErrorText.Text = "Please select where to save your meeting notes";
                ValidationErrorText.Visibility = Visibility.Visible;
                DropdownSpacer.Visibility = Visibility.Collapsed;
                return;
            }

            SelectedDatabase = DatabaseComboBox.SelectedItem as NotionDatabase;

            // Find the workspace that owns this database
            NotionWorkspaceIntegration? owningWorkspace = null;
            foreach (var workspace in AvailableWorkspaces)
            {
                if (workspace.Databases != null)
                {
                    foreach (var db in workspace.Databases)
                    {
                        if (db.Id == SelectedDatabase?.Id)
                        {
                            owningWorkspace = workspace;
                            break;
                        }
                    }
                }
                if (owningWorkspace != null) break;
            }

            if (owningWorkspace == null)
            {
                ValidationErrorText.Text = "Could not find the workspace for this database. Try reconnecting in Settings.";
                ValidationErrorText.Visibility = Visibility.Visible;
                DropdownSpacer.Visibility = Visibility.Collapsed;
                return;
            }

            SelectedWorkspace = owningWorkspace;

            // Create meeting info with selected workspace
            var meetingInfo = new MeetingInfo
            {
                Date = DateTime.Today,
                Title = "Meeting Notes",
                Organizer = "",
                Attendees = "",
                Comments = "",
                Workspace = owningWorkspace,
                StartTime = DateTime.Now
            };

            // Open note-taking window directly
            var noteWindow = new NoteTakingWindow(meetingInfo);
            noteWindow.Show();
        }

        private void OnConnectNotionClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(AvailableWorkspaces);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();

                // Refresh after settings changes
                RefreshDatabaseList();
                _ = LoadRecentNotesFromNotion(AvailableWorkspaces);
            }
            catch (Exception ex)
            {
                // Error opening settings
            }
        }

        private void OnDatabaseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear validation error when user makes a selection
            if (DatabaseComboBox.SelectedItem != null)
            {
                ValidationErrorText.Visibility = Visibility.Collapsed;
                DropdownSpacer.Visibility = Visibility.Visible;
            }
        }

        private void UpdateStatus()
        {
            if (IsCallDetectionEnabled)
            {
                StatusText = "Monitoring";
                StatusDescription = "Listening for calls from Teams, Zoom, and Meet";
                StatusColor = (Brush)FindResource("SuccessBrush");
            }
            else
            {
                StatusText = "Disabled";
                StatusDescription = "Call detection is turned off";
                StatusColor = (Brush)FindResource("WarningBrush");
            }
        }

        private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(AvailableWorkspaces);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
                
                // Refresh database list after settings changes
                RefreshDatabaseList();
                
                // Reload recent notes from Notion
                _ = LoadRecentNotesFromNotion(AvailableWorkspaces);
            }
            catch (Exception ex)
            {
                // Error opening settings
            }
        }

        private void OnSimulateCallClicked(object sender, RoutedEventArgs e)
        {
            _userStartedNotes = false;
            _callDetectionService.ScanNow();

            if (!_callDetectionService.IsMeetingActive && !IsDetectionBannerVisible)
            {
                DetectionBannerTitle = "No Active Calls Detected";
                DetectionBannerMessage = "No monitored call apps are currently in a meeting. Start a Zoom call and try again.";
                DetectionBannerColor = (Brush)FindResource("WarningBrush");
                DetectionFoundApps = false;
                IsDetectionBannerVisible = true;

                StatusText = "No Calls";
                StatusDescription = "No active call apps found";
                StatusColor = (Brush)FindResource("WarningBrush");
            }
        }

        private void OnDismissDetectionBanner(object sender, RoutedEventArgs e)
        {
            IsDetectionBannerVisible = false;
            _userStartedNotes = false;

            if (_callDetectionService.IsMeetingActive)
            {
                StatusText = "Call Detected";
                StatusDescription = $"{_callDetectionService.ActivePlatformName} meeting in progress";
                StatusColor = (Brush)FindResource("InfoBrush");
            }
            else
            {
                UpdateStatus();
            }
        }

        private void OnStartNotesFromDetection(object sender, RoutedEventArgs e)
        {
            _userStartedNotes = true;
            IsDetectionBannerVisible = false;
            OnStartNotesClicked(sender, e);
        }

        private void OnMeetingDetected(object? sender, MeetingDetectedEventArgs e)
        {
            if (_userStartedNotes) return;

            DetectionBannerTitle = $"{e.PlatformName} Meeting Detected";
            DetectionBannerMessage = $"A {e.PlatformName} meeting is in progress.\nDetected via: {e.DetectionMethod}";
            DetectionBannerColor = (Brush)FindResource("InfoBrush");
            DetectionFoundApps = true;
            IsDetectionBannerVisible = true;

            StatusText = "Call Detected";
            StatusDescription = $"{e.PlatformName} meeting in progress";
            StatusColor = (Brush)FindResource("InfoBrush");
        }

        private void OnMeetingEnded(object? sender, MeetingEndedEventArgs e)
        {
            if (_userStartedNotes) return;

            if (IsDetectionBannerVisible)
            {
                DetectionBannerTitle = $"{e.PlatformName} Meeting Ended";
                DetectionBannerMessage = "The meeting appears to have ended.";
                DetectionBannerColor = (Brush)FindResource("WarningBrush");
                DetectionFoundApps = false;
            }

            StatusText = "Monitoring";
            StatusDescription = "Listening for calls from Teams, Zoom, and Meet";
            StatusColor = (Brush)FindResource("SuccessBrush");
        }

        private void OnDetectionError(object? sender, DetectionErrorEventArgs e)
        {
            DetectionBannerTitle = "Detection Error";
            DetectionBannerMessage = e.ErrorMessage;
            DetectionBannerColor = (Brush)FindResource("ErrorBrush");
            DetectionFoundApps = false;
            IsDetectionBannerVisible = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            _callDetectionService?.Dispose();
            base.OnClosed(e);
        }

        private void OnTestNotionClicked(object sender, RoutedEventArgs e)
        {
            // Connection test completed
        }

        private void OnTestLLamaSharpClicked(object sender, RoutedEventArgs e)
        {
            var debugWindow = new LLamaSharpDebugWindow();
            debugWindow.Owner = this;
            debugWindow.Show();
        }

        private void OnRecentNoteClicked(object sender, SelectionChangedEventArgs e)
        {
            if (RecentNotesListBox.SelectedItem is RecentNote selectedNote && !string.IsNullOrEmpty(selectedNote.NotionUrl))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = selectedNote.NotionUrl,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Handle error silently
                }
            }
            
            // Clear selection so user can click the same item again
            RecentNotesListBox.SelectedItem = null;
        }


        public static async Task LoadRecentNotesFromNotion(ObservableCollection<NotionWorkspaceIntegration> availableWorkspaces)
        {
            try
            {
                // Get the first available workspace with a database
                var workspace = availableWorkspaces.FirstOrDefault(w => w.SelectedDatabase != null);
                if (workspace == null) return;

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workspace.ApiKey);
                httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

                var requestBody = new
                {
                    filter = new
                    {
                        property = "Meeting Title",
                        title = new { is_not_empty = true }
                    },
                    sorts = new[]
                    {
                        new { property = "Created time", direction = "descending" }
                    },
                    page_size = 10
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"https://api.notion.com/v1/databases/{workspace.SelectedDatabase.Id}/query", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    RecentNotes.Clear();
                    
                    foreach (var result in responseJson.GetProperty("results").EnumerateArray())
                    {
                        var pageId = result.GetProperty("id").GetString();
                        var properties = result.GetProperty("properties");
                        
                        // Extract title
                        string title = "Untitled";
                        if (properties.TryGetProperty("Meeting Title", out var titleProp) && 
                            titleProp.TryGetProperty("title", out var titleArray) && 
                            titleArray.GetArrayLength() > 0)
                        {
                            title = titleArray[0].GetProperty("plain_text").GetString();
                        }
                        
                        // Extract preview from notes
                        string preview = "No preview available";
                        if (properties.TryGetProperty("Your Notes", out var notesProp) && 
                            notesProp.TryGetProperty("rich_text", out var notesArray) && 
                            notesArray.GetArrayLength() > 0)
                        {
                            var notesText = notesArray[0].GetProperty("plain_text").GetString();
                            preview = notesText.Length > 100 ? notesText.Substring(0, 100) + "..." : notesText;
                        }
                        
                        // Extract creation date
                        string date = "Unknown date";
                        if (result.TryGetProperty("created_time", out var createdTime))
                        {
                            var created = DateTime.Parse(createdTime.GetString());
                            date = created.ToString("MMM dd, yyyy 'at' h:mm tt");
                        }
                        
                        var recentNote = new RecentNote
                        {
                            Title = title,
                            Date = date,
                            Preview = preview,
                            NotionUrl = $"https://notion.so/{pageId.Replace("-", "")}",
                            NotionPageId = pageId
                        };
                        
                        RecentNotes.Add(recentNote);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error silently - recent notes are not critical functionality
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DetectedApp
    {
        public string AppName { get; set; }
        public bool IsEnabled { get; set; }
        public string Status { get; set; }
    }

    public class RecentNote
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string Preview { get; set; }
        public string NotionUrl { get; set; }
        public string NotionPageId { get; set; }
    }

}

