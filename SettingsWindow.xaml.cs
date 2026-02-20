using System;
using System.Collections.Generic;
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
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private bool _isCallDetectionEnabled = true;
        private bool _startMinimized = false;
        private bool _autoSaveNotes = true;
        private bool _showNotifications = true;
        private bool _isFormVisible = false;
        private string _formTitle = "Add Workspace";
        private NotionWorkspaceIntegration _editingWorkspace;
        private ObservableCollection<NotionWorkspaceIntegration> _workspaceIntegrations;
        private ObservableCollection<DetectedApp> _detectedApps;
        private ObservableCollection<NotionDatabase> _availableDatabases;

        public SettingsWindow(ObservableCollection<NotionWorkspaceIntegration> sharedWorkspaces = null)
        {
            InitializeComponent();
            DataContext = this;

            // Initialize collections
            // Use shared workspace list from MainWindow, or create new if not provided
            WorkspaceIntegrations = sharedWorkspaces ?? new ObservableCollection<NotionWorkspaceIntegration>();
            
            // Load app settings
            AppSettings.LoadSettings();

            DetectedApps = new ObservableCollection<DetectedApp>
            {
                new DetectedApp { AppName = "Microsoft Teams", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Zoom", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Google Meet", IsEnabled = true, Status = "Monitoring" },
                new DetectedApp { AppName = "Discord", IsEnabled = false, Status = "Disabled" }
            };

            // Initialize empty databases list - will be populated when testing workspace connection
            AvailableDatabases = new ObservableCollection<NotionDatabase>();

            // Set up list boxes
            WorkspaceListBox.ItemsSource = WorkspaceIntegrations;
            DetectedAppsListBox.ItemsSource = DetectedApps;
            DatabaseComboBox.ItemsSource = AvailableDatabases;

            // Initialize editing workspace
            EditingWorkspace = new NotionWorkspaceIntegration();
        }

        public bool IsCallDetectionEnabled
        {
            get => _isCallDetectionEnabled;
            set
            {
                _isCallDetectionEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                _startMinimized = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSaveNotes
        {
            get => _autoSaveNotes;
            set
            {
                _autoSaveNotes = value;
                OnPropertyChanged();
            }
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set
            {
                _showNotifications = value;
                OnPropertyChanged();
            }
        }

        public bool IsFormVisible
        {
            get => _isFormVisible;
            set
            {
                _isFormVisible = value;
                OnPropertyChanged();
            }
        }

        public string FormTitle
        {
            get => _formTitle;
            set
            {
                _formTitle = value;
                OnPropertyChanged();
            }
        }

        public NotionWorkspaceIntegration EditingWorkspace
        {
            get => _editingWorkspace;
            set
            {
                _editingWorkspace = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NotionWorkspaceIntegration> WorkspaceIntegrations
        {
            get => _workspaceIntegrations;
            set
            {
                _workspaceIntegrations = value;
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

        public ObservableCollection<NotionDatabase> AvailableDatabases
        {
            get => _availableDatabases;
            set
            {
                _availableDatabases = value;
                OnPropertyChanged();
            }
        }

        private void OnAddWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            FormTitle = "Add Workspace";
            EditingWorkspace = new NotionWorkspaceIntegration();
            IsFormVisible = true;
        }

        private void OnEditWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotionWorkspaceIntegration workspace)
            {
                FormTitle = "Edit Workspace";
                EditingWorkspace = new NotionWorkspaceIntegration
                {
                    WorkspaceName = workspace.WorkspaceName,
                    WorkspaceId = workspace.WorkspaceId,
                    ApiKey = workspace.ApiKey,
                    SelectedDatabase = workspace.SelectedDatabase
                };
                IsFormVisible = true;
            }
        }

        private async void OnFetchDatabasesClicked(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyBox.Password;
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }
            
            try
            {
                // Disable the button while fetching
                if (sender is Button button)
                {
                    button.IsEnabled = false;
                    button.Content = "Fetching...";
                }
                
                // Fetch databases from Notion API
                var databases = await FetchNotionDatabasesAsync(apiKey);
                
                // Clear and populate the AvailableDatabases list
                AvailableDatabases.Clear();
                foreach (var db in databases)
                {
                    AvailableDatabases.Add(db);
                }
                
                // Re-enable the button
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Fetch Databases";
                }
            }
            catch (Exception ex)
            {
                // Failed to fetch databases
                    
                // Re-enable the button
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Fetch Databases";
                }
            }
        }
        
        private async Task<List<NotionDatabase>> FetchNotionDatabasesAsync(string apiKey)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            
            var databases = new List<NotionDatabase>();
            string? nextCursor = null;
            
            // Fetch all databases with pagination
            do
            {
                var requestBody = nextCursor == null
                    ? "{\"filter\":{\"value\":\"database\",\"property\":\"object\"},\"page_size\":100}"
                    : $"{{\"filter\":{{\"value\":\"database\",\"property\":\"object\"}},\"page_size\":100,\"start_cursor\":\"{nextCursor}\"}}";
                
                var response = await httpClient.PostAsync("https://api.notion.com/v1/search", 
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Notion API returned {response.StatusCode}: {errorContent}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var results = jsonDoc.RootElement.GetProperty("results");
                
                foreach (var result in results.EnumerateArray())
                {
                    var id = result.GetProperty("id").GetString() ?? "";
                    var objectType = result.GetProperty("object").GetString() ?? "";
                    var title = "";
                    
                    // Extract title from the database
                    if (result.TryGetProperty("title", out var titleArray) && titleArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var titleObj in titleArray.EnumerateArray())
                        {
                            if (titleObj.TryGetProperty("plain_text", out var plainText))
                            {
                                title += plainText.GetString();
                            }
                        }
                    }
                    
                    // Only add databases/pages that contain "Meetings" in the name
                    var displayName = string.IsNullOrWhiteSpace(title) ? $"Untitled ({id[..8]})" : title;
                    if (displayName.Contains("Meetings", StringComparison.OrdinalIgnoreCase))
                    {
                        databases.Add(new NotionDatabase
                        {
                            Name = displayName,
                            Id = id,
                            Type = objectType == "database" ? "Database" : "Page"
                        });
                    }
                }
                
                // Check for next page
                if (jsonDoc.RootElement.TryGetProperty("has_more", out var hasMore) && hasMore.GetBoolean())
                {
                    nextCursor = jsonDoc.RootElement.GetProperty("next_cursor").GetString();
                }
                else
                {
                    nextCursor = null;
                }
            }
            while (nextCursor != null);
            
            // Also fetch pages (in case they want to save to a page)
            nextCursor = null;
            do
            {
                var requestBody = nextCursor == null
                    ? "{\"filter\":{\"value\":\"page\",\"property\":\"object\"},\"page_size\":100}"
                    : $"{{\"filter\":{{\"value\":\"page\",\"property\":\"object\"}},\"page_size\":100,\"start_cursor\":\"{nextCursor}\"}}";
                
                var response = await httpClient.PostAsync("https://api.notion.com/v1/search", 
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var results = jsonDoc.RootElement.GetProperty("results");
                    
                    foreach (var result in results.EnumerateArray())
                    {
                        var id = result.GetProperty("id").GetString() ?? "";
                        var title = "";
                        
                        // Extract title from properties
                        if (result.TryGetProperty("properties", out var properties))
                        {
                            // Look for title property
                            foreach (var prop in properties.EnumerateObject())
                            {
                                if (prop.Value.TryGetProperty("title", out var titleArray) && titleArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var titleObj in titleArray.EnumerateArray())
                                    {
                                        if (titleObj.TryGetProperty("plain_text", out var plainText))
                                        {
                                            title += plainText.GetString();
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        
                        // Only add pages that contain "Meetings" in the name
                        var displayName = string.IsNullOrWhiteSpace(title) ? $"Untitled Page ({id[..8]})" : $"{title} (Page)";
                        if (displayName.Contains("Meetings", StringComparison.OrdinalIgnoreCase))
                        {
                            databases.Add(new NotionDatabase
                            {
                                Name = displayName,
                                Id = id,
                                Type = "Page"
                            });
                        }
                    }
                    
                    // Check for next page
                    if (jsonDoc.RootElement.TryGetProperty("has_more", out var hasMore) && hasMore.GetBoolean())
                    {
                        nextCursor = jsonDoc.RootElement.GetProperty("next_cursor").GetString();
                    }
                    else
                    {
                        nextCursor = null;
                    }
                }
                else
                {
                    nextCursor = null;
                }
            }
            while (nextCursor != null);
            
            return databases;
        }

        private void OnTestWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotionWorkspaceIntegration workspace)
            {
                // Testing connection
            }
        }

        private void OnDeleteWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotionWorkspaceIntegration workspace)
            {
                WorkspaceIntegrations.Remove(workspace);
                SaveWorkspaces(WorkspaceIntegrations); // Save after deletion
            }
        }

        private void OnSaveWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(WorkspaceNameBox.Text))
            {
                return;
            }

            if (string.IsNullOrEmpty(ApiKeyBox.Password))
            {
                return;
            }

            if (DatabaseComboBox.SelectedItem == null)
            {
                return;
            }

            // Create or update workspace integration
            var workspace = new NotionWorkspaceIntegration
            {
                WorkspaceName = WorkspaceNameBox.Text,
                WorkspaceId = $"workspace-{Guid.NewGuid().ToString("N")[..8]}",
                ApiKey = ApiKeyBox.Password,
                SelectedDatabase = DatabaseComboBox.SelectedItem as NotionDatabase,
                Databases = new ObservableCollection<NotionDatabase>(AvailableDatabases),
                StatusText = "Connected",
                StatusColor = Brushes.Green
            };

            // Add or update in collection
            if (FormTitle == "Add Workspace")
            {
                WorkspaceIntegrations.Add(workspace);
            }
            else
            {
                // Find and update existing workspace
                var existingIndex = WorkspaceIntegrations.ToList().FindIndex(w => w.WorkspaceName == EditingWorkspace.WorkspaceName);
                if (existingIndex >= 0)
                {
                    WorkspaceIntegrations[existingIndex] = workspace;
                }
            }

            // Save to persistent storage
            SaveWorkspaces(WorkspaceIntegrations);

            // Clear the form
            WorkspaceNameBox.Text = "";
            ApiKeyBox.Password = "";
            AvailableDatabases.Clear();
            
            IsFormVisible = false;
        }

        private void OnCancelWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            IsFormVisible = false;
        }

        private void OnCallDetectionChanged(object sender, RoutedEventArgs e)
        {
            IsCallDetectionEnabled = CallDetectionCheckBox.IsChecked ?? false;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnTestLMStudioClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                var requestBody = new
                {
                    model = "meta-llama-3.1-8b-instruct",
                    messages = new[]
                    {
                        new { role = "user", content = "Hello, can you respond with 'LMStudio connection successful'?" }
                    },
                    max_tokens = 50,
                    temperature = 0.1
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("http://127.0.0.1:1234/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var aiResponse = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                    
                    MessageBox.Show($"LMStudio connection successful!\n\nResponse: {aiResponse}", "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"LMStudio connection failed. Status: {response.StatusCode}\n\nMake sure LMStudio is running on http://127.0.0.1:1234 with meta-llama-3.1-8b-instruct loaded.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LMStudio connection failed: {ex.Message}\n\nMake sure LMStudio is running on http://127.0.0.1:1234 with meta-llama-3.1-8b-instruct loaded.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string GetWorkspacesFilePath()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingNotesApp");
            Directory.CreateDirectory(appDataPath); // Ensure directory exists
            return Path.Combine(appDataPath, "workspaces.json");
        }

        public static void SaveWorkspaces(ObservableCollection<NotionWorkspaceIntegration> workspaces)
        {
            try
            {
                var serializableWorkspaces = workspaces.Select(w => new SerializableWorkspace
                {
                    WorkspaceName = w.WorkspaceName,
                    WorkspaceId = w.WorkspaceId,
                    ApiKey = w.ApiKey,
                    SelectedDatabase = w.SelectedDatabase,
                    Databases = w.Databases?.ToList() ?? new List<NotionDatabase>(),
                    StatusText = w.StatusText
                }).ToList();

                var json = JsonSerializer.Serialize(serializableWorkspaces, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(GetWorkspacesFilePath(), json);
            }
            catch (Exception ex)
            {
                // Failed to save workspaces
            }
        }

        public static ObservableCollection<NotionWorkspaceIntegration> LoadWorkspaces()
        {
            try
            {
                var filePath = GetWorkspacesFilePath();
                if (!File.Exists(filePath))
                {
                    return new ObservableCollection<NotionWorkspaceIntegration>();
                }

                var json = File.ReadAllText(filePath);
                var serializableWorkspaces = JsonSerializer.Deserialize<List<SerializableWorkspace>>(json);

                if (serializableWorkspaces == null)
                {
                    return new ObservableCollection<NotionWorkspaceIntegration>();
                }

                var workspaces = new ObservableCollection<NotionWorkspaceIntegration>();
                foreach (var sw in serializableWorkspaces)
                {
                    workspaces.Add(new NotionWorkspaceIntegration
                    {
                        WorkspaceName = sw.WorkspaceName,
                        WorkspaceId = sw.WorkspaceId,
                        ApiKey = sw.ApiKey,
                        SelectedDatabase = sw.SelectedDatabase,
                        Databases = new ObservableCollection<NotionDatabase>(sw.Databases ?? new List<NotionDatabase>()),
                        StatusText = sw.StatusText ?? "Connected",
                        StatusColor = Brushes.Green
                    });
                }

                return workspaces;
            }
            catch (Exception ex)
            {
                // Failed to load workspaces
                return new ObservableCollection<NotionWorkspaceIntegration>();
            }
        }
    }

    // Serializable version without Brush (which can't be serialized)
    public class SerializableWorkspace
    {
        public string WorkspaceName { get; set; }
        public string WorkspaceId { get; set; }
        public string ApiKey { get; set; }
        public NotionDatabase SelectedDatabase { get; set; }
        public List<NotionDatabase> Databases { get; set; }
        public string StatusText { get; set; }
    }

    public class NotionWorkspaceIntegration
    {
        public string WorkspaceName { get; set; }
        public string WorkspaceId { get; set; }
        public string ApiKey { get; set; }
        public NotionDatabase SelectedDatabase { get; set; }
        public ObservableCollection<NotionDatabase> Databases { get; set; }
        public string StatusText { get; set; }
        public Brush StatusColor { get; set; }
    }

    public class NotionDatabase
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Type { get; set; } // "Database" or "Page"
    }

    public static class AppSettings
    {
        private static string GetSettingsFilePath()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingNotesApp");
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, "appsettings.json");
        }
        
        public static void SaveSettings()
        {
            var settings = new { };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetSettingsFilePath(), json);
        }
        
        public static void LoadSettings()
        {
            var filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var settings = JsonSerializer.Deserialize<JsonElement>(json);
                    // No settings to load for LMStudio
                }
                catch
                {
                    // If loading fails, use defaults
                }
            }
        }
    }
}
