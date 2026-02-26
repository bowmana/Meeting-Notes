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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

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
        private CancellationTokenSource? _diarizationDownloadCts;
        private CancellationTokenSource? _asrDownloadCts;

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

            // Initialize diarization UI state
            RefreshDiarizationModelList();
            UpdateEmbeddingModelStatus();
            DiarizationNumSpeakersBox.Text = AppSettings.Diarization.NumSpeakers.ToString();
            DiarizationThresholdBox.Text = AppSettings.Diarization.Threshold.ToString("F1");

            // Initialize ASR model list UI
            RefreshASRModelList();
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
                MessageBox.Show("Please enter your Notion API key first.", "API Key Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fetchButton = sender as Button;
            try
            {
                // Disable the button while fetching
                if (fetchButton != null)
                {
                    fetchButton.IsEnabled = false;
                    fetchButton.Content = "Fetching...";
                }

                // Fetch databases from Notion API
                var databases = await FetchNotionDatabasesAsync(apiKey);

                // Clear and populate the AvailableDatabases list
                AvailableDatabases.Clear();
                foreach (var db in databases)
                {
                    AvailableDatabases.Add(db);
                }

                if (databases.Count == 0)
                {
                    MessageBox.Show(
                        "No databases found. Make sure your integration has been added as a connection to at least one database in Notion.\n\n" +
                        "In Notion: open a database page → click ••• → Add connections → select your integration.",
                        "No Databases Found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch databases from Notion:\n\n{ex.Message}", "Fetch Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (fetchButton != null)
                {
                    fetchButton.IsEnabled = true;
                    fetchButton.Content = "Fetch Databases";
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

                    var displayName = string.IsNullOrWhiteSpace(title) ? $"Untitled ({id[..8]})" : title;
                    databases.Add(new NotionDatabase
                    {
                        Name = displayName,
                        Id = id,
                        Type = "Database"
                    });
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

            // Sort: databases with "Meetings" in the name come first, then alphabetical
            databases.Sort((a, b) =>
            {
                bool aMeetings = a.Name.Contains("Meeting", StringComparison.OrdinalIgnoreCase);
                bool bMeetings = b.Name.Contains("Meeting", StringComparison.OrdinalIgnoreCase);
                if (aMeetings && !bMeetings) return -1;
                if (!aMeetings && bMeetings) return 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

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

        private void OnNotionIntegrationLinkClicked(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
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

        // --- Speaker Diarization Handlers ---

        private ObservableCollection<DiarizationSegmentationModelViewModel> _diarizationModels = new();

        private void RefreshDiarizationModelList()
        {
            _diarizationModels.Clear();
            foreach (var def in DiarizationModelDefinition.AllModels)
            {
                _diarizationModels.Add(new DiarizationSegmentationModelViewModel(def));
            }
            DiarizationModelListControl.ItemsSource = _diarizationModels;

            // Populate active model combobox with downloaded models only
            var downloaded = _diarizationModels.Where(m => m.IsDownloaded).ToList();
            ActiveDiarizationModelComboBox.ItemsSource = downloaded;

            var current = downloaded.FirstOrDefault(m => m.ModelType == AppSettings.Diarization.SelectedSegmentationModel)
                          ?? downloaded.FirstOrDefault();
            ActiveDiarizationModelComboBox.SelectedItem = current;
        }

        private void OnActiveDiarizationModelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveDiarizationModelComboBox.SelectedItem is DiarizationSegmentationModelViewModel vm)
            {
                AppSettings.Diarization.SelectedSegmentationModel = vm.ModelType;
                AppSettings.SaveSettings();
            }
        }

        private void UpdateEmbeddingModelStatus()
        {
            if (DiarizationModelManager.IsEmbeddingModelDownloaded)
            {
                EmbeddingModelStatusDot.Fill = Brushes.Green;
                EmbeddingModelStatusText.Text = "Downloaded";
                DownloadEmbeddingModelButton.Visibility = Visibility.Collapsed;
                DeleteEmbeddingModelButton.Visibility = Visibility.Visible;
            }
            else
            {
                EmbeddingModelStatusDot.Fill = Brushes.Gray;
                EmbeddingModelStatusText.Text = "Not downloaded";
                DownloadEmbeddingModelButton.Visibility = Visibility.Visible;
                DeleteEmbeddingModelButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnDownloadDiarizationSegModelClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not DiarizationSegmentationModelType modelType)
                return;

            _diarizationDownloadCts = new CancellationTokenSource();
            DiarizationSegDownloadProgress.Visibility = Visibility.Visible;

            var modelManager = new DiarizationModelManager();
            var progress = new Progress<(double percent, string status)>(p =>
            {
                DiarizationSegDownloadProgressBar.Value = p.percent;
                DiarizationSegDownloadStatusText.Text = p.status;
            });

            try
            {
                await modelManager.DownloadSegmentationModelAsync(modelType, progress, _diarizationDownloadCts.Token);
                RefreshDiarizationModelList();
            }
            catch (OperationCanceledException)
            {
                DiarizationSegDownloadStatusText.Text = "Download cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download model: {ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DiarizationSegDownloadProgress.Visibility = Visibility.Collapsed;
                _diarizationDownloadCts?.Dispose();
                _diarizationDownloadCts = null;
            }
        }

        private void OnCancelDiarizationSegDownloadClicked(object sender, RoutedEventArgs e)
        {
            _diarizationDownloadCts?.Cancel();
        }

        private void OnDeleteDiarizationSegModelClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not DiarizationSegmentationModelType modelType)
                return;

            var def = DiarizationModelDefinition.Get(modelType);
            var result = MessageBox.Show(
                $"Delete {def.DisplayName}? You can re-download it at any time.",
                "Delete Model", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DiarizationModelManager.DeleteSegmentationModel(modelType);

                    if (AppSettings.Diarization.SelectedSegmentationModel == modelType)
                    {
                        var remaining = DiarizationModelManager.GetDownloadedSegmentationModels();
                        if (remaining.Count > 0)
                            AppSettings.Diarization.SelectedSegmentationModel = remaining.First();
                        AppSettings.SaveSettings();
                    }

                    RefreshDiarizationModelList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete model: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnDownloadEmbeddingModelClicked(object sender, RoutedEventArgs e)
        {
            _diarizationDownloadCts = new CancellationTokenSource();
            DownloadEmbeddingModelButton.IsEnabled = false;
            EmbeddingDownloadProgress.Visibility = Visibility.Visible;

            var modelManager = new DiarizationModelManager();
            var progress = new Progress<(double percent, string status)>(p =>
            {
                EmbeddingDownloadProgressBar.Value = p.percent;
                EmbeddingDownloadStatusText.Text = p.status;
            });

            try
            {
                await modelManager.DownloadEmbeddingModelAsync(progress, _diarizationDownloadCts.Token);
                UpdateEmbeddingModelStatus();
            }
            catch (OperationCanceledException)
            {
                EmbeddingDownloadStatusText.Text = "Download cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download embedding model: {ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadEmbeddingModelButton.IsEnabled = true;
                EmbeddingDownloadProgress.Visibility = Visibility.Collapsed;
                _diarizationDownloadCts?.Dispose();
                _diarizationDownloadCts = null;
            }
        }

        private void OnDeleteEmbeddingModelClicked(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Delete speaker embedding model? You can re-download it at any time (~28 MB).",
                "Delete Model", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DiarizationModelManager.DeleteEmbeddingModel();
                    UpdateEmbeddingModelStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete model: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- Speech Recognition (ASR) Handlers ---

        private ObservableCollection<ASRModelViewModel> _asrModels = new();
        public ObservableCollection<ASRModelViewModel> ASRModels => _asrModels;

        private void RefreshASRModelList()
        {
            _asrModels.Clear();
            foreach (var def in ASRModelDefinition.AllModels)
            {
                _asrModels.Add(new ASRModelViewModel(def));
            }
            ASRModelListControl.ItemsSource = _asrModels;

            // Refresh active model ComboBox — only show downloaded models
            var downloaded = _asrModels.Where(m => m.IsDownloaded).ToList();
            ActiveASRModelComboBox.ItemsSource = downloaded;
            ActiveASRModelComboBox.DisplayMemberPath = "DisplayName";

            // Select the currently persisted model if it's downloaded
            var current = downloaded.FirstOrDefault(m => m.ModelType == AppSettings.ASR.SelectedModel)
                          ?? downloaded.FirstOrDefault();

            ActiveASRModelComboBox.SelectedItem = current;
        }

        private void OnActiveASRModelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveASRModelComboBox.SelectedItem is ASRModelViewModel vm)
            {
                AppSettings.ASR.SelectedModel = vm.ModelType;
                AppSettings.SaveSettings();
            }
        }

        private async void OnDownloadASRModelClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ASRModelType modelType)
                return;

            _asrDownloadCts = new CancellationTokenSource();
            ASRDownloadProgress.Visibility = Visibility.Visible;

            var modelManager = new ASRModelManager();
            var progress = new Progress<(double percent, string status)>(p =>
            {
                ASRDownloadProgressBar.Value = p.percent;
                ASRDownloadStatusText.Text = p.status;
            });

            try
            {
                await modelManager.DownloadModelsAsync(modelType, progress, _asrDownloadCts.Token);
                RefreshASRModelList();
            }
            catch (OperationCanceledException)
            {
                ASRDownloadStatusText.Text = "Download cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download model: {ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ASRDownloadStatusText.Text = $"Download failed: {ex.Message}";
            }
            finally
            {
                ASRDownloadProgress.Visibility = Visibility.Collapsed;
                _asrDownloadCts?.Dispose();
                _asrDownloadCts = null;
            }
        }

        private void OnCancelASRDownloadClicked(object sender, RoutedEventArgs e)
        {
            _asrDownloadCts?.Cancel();
        }

        private void OnDeleteASRModelClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ASRModelType modelType)
                return;

            var def = ASRModelDefinition.Get(modelType);
            var result = MessageBox.Show(
                $"Delete {def.DisplayName}? You can re-download it at any time ({def.SizeText}).",
                "Delete Model", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ASRModelManager.DeleteModels(modelType);

                    // If we deleted the active model, switch to another downloaded model
                    if (AppSettings.ASR.SelectedModel == modelType)
                    {
                        var remaining = ASRModelManager.GetDownloadedModels();
                        AppSettings.ASR.SelectedModel = remaining.FirstOrDefault();
                        AppSettings.SaveSettings();
                    }

                    RefreshASRModelList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete model: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Save diarization settings before closing
            if (int.TryParse(DiarizationNumSpeakersBox.Text, out var numSpeakers))
                AppSettings.Diarization.NumSpeakers = numSpeakers;

            if (float.TryParse(DiarizationThresholdBox.Text, out var threshold))
                AppSettings.Diarization.Threshold = Math.Clamp(threshold, 0f, 1f);

            AppSettings.SaveSettings();
            base.OnClosing(e);
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

    /// <summary>
    /// ViewModel for displaying ASR models in the Settings model browser.
    /// </summary>
    public class ASRModelViewModel
    {
        public ASRModelType ModelType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SizeText { get; }
        public bool IsDownloaded { get; }
        public string StatusText => IsDownloaded ? "Downloaded" : "Not downloaded";
        public Brush StatusColor => IsDownloaded ? Brushes.Green : Brushes.Gray;
        public Visibility DownloadButtonVisibility => IsDownloaded ? Visibility.Collapsed : Visibility.Visible;
        public Visibility DeleteButtonVisibility => IsDownloaded ? Visibility.Visible : Visibility.Collapsed;

        public ASRModelViewModel(ASRModelDefinition def)
        {
            ModelType = def.ModelType;
            DisplayName = def.DisplayName;
            Description = def.Description;
            SizeText = def.SizeText;
            IsDownloaded = ASRModelManager.AreModelsDownloaded(def.ModelType);
        }
    }

    /// <summary>
    /// ViewModel for displaying diarization segmentation models in the Settings model browser.
    /// </summary>
    public class DiarizationSegmentationModelViewModel
    {
        public DiarizationSegmentationModelType ModelType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SizeText { get; }
        public bool IsDownloaded { get; }
        public string StatusText => IsDownloaded ? "Downloaded" : "Not downloaded";
        public Brush StatusColor => IsDownloaded ? Brushes.Green : Brushes.Gray;
        public Visibility DownloadButtonVisibility => IsDownloaded ? Visibility.Collapsed : Visibility.Visible;
        public Visibility DeleteButtonVisibility => IsDownloaded ? Visibility.Visible : Visibility.Collapsed;

        public DiarizationSegmentationModelViewModel(DiarizationModelDefinition def)
        {
            ModelType = def.ModelType;
            DisplayName = def.DisplayName;
            Description = def.Description;
            SizeText = def.SizeText;
            IsDownloaded = DiarizationModelManager.IsSegmentationModelDownloaded(def.ModelType);
        }
    }

    public class DiarizationSettings
    {
        public int NumSpeakers { get; set; } = -1;         // -1 = auto-detect number of speakers
        public float Threshold { get; set; } = 0.7f;       // Clustering threshold (lower = more speakers, higher = fewer)
        public float MinDurationOn { get; set; } = 0.5f;   // Minimum speech segment duration in seconds
        public float MinDurationOff { get; set; } = 0.5f;  // Minimum silence gap between segments in seconds
        public DiarizationSegmentationModelType SelectedSegmentationModel { get; set; } = DiarizationSegmentationModelType.Pyannote3;
    }

    public class ASRSettings
    {
        public ASRModelType SelectedModel { get; set; } = ASRModelType.MoonshineTiny;
    }

    public static class AppSettings
    {
        public static DiarizationSettings Diarization { get; set; } = new DiarizationSettings();
        public static ASRSettings ASR { get; set; } = new ASRSettings();

        private static string GetSettingsFilePath()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingNotesApp");
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, "appsettings.json");
        }

        public static void SaveSettings()
        {
            var settings = new
            {
                Diarization = Diarization,
                ASR = ASR
            };
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
                    var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("Diarization", out var diarizationElement))
                    {
                        var diarization = JsonSerializer.Deserialize<DiarizationSettings>(diarizationElement.GetRawText());
                        if (diarization != null)
                            Diarization = diarization;
                    }

                    if (doc.RootElement.TryGetProperty("ASR", out var asrElement))
                    {
                        var asr = JsonSerializer.Deserialize<ASRSettings>(asrElement.GetRawText());
                        if (asr != null)
                            ASR = asr;
                    }
                }
                catch
                {
                    // If loading fails, use defaults
                }
            }
        }
    }
}
