using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            
            // Force black text color on DatabaseComboBox
            DatabaseComboBox.Loaded += (s, e) => {
                DatabaseComboBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                System.Windows.Documents.TextElement.SetForeground(DatabaseComboBox, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black));
            };

            // Populate databases from all workspaces
            RefreshDatabaseList();
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
            StatusText = "Recording";
            StatusDescription = "Taking notes for current call";
            StatusColor = (Brush)FindResource("InfoBrush");
        }

        private void OnTestNotionClicked(object sender, RoutedEventArgs e)
        {
            // Connection test completed
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

