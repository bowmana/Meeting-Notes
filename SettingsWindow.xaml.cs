using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using MeetingNotesApp.Models;

namespace MeetingNotesApp
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private bool _isCallDetectionEnabled = true;
        private bool _startMinimized = false;
        private bool _autoSaveNotes = true;
        private bool _showNotifications = true;
        private bool _isFormVisible = false;
        private bool _isPickerVisible = false;
        private string _formTitle = "Add Integration";
        private IntegrationProviderType? _selectedProviderType;
        private NotionIntegration _editingIntegration;
        private ObservableCollection<Integration> _integrations;
        private ObservableCollection<DetectedApp> _detectedApps;
        private ObservableCollection<NotionDatabase> _availableDatabases;
        private CancellationTokenSource? _diarizationDownloadCts;
        private CancellationTokenSource? _asrDownloadCts;
        private string _selectedSettingsPage = "Integrations";

        public SettingsWindow(ObservableCollection<Integration> sharedIntegrations = null)
        {
            InitializeComponent();
            DataContext = this;

            // Initialize collections
            // Use shared integration list from MainWindow, or create new if not provided
            Integrations = sharedIntegrations ?? new ObservableCollection<Integration>();

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
            WorkspaceListBox.ItemsSource = Integrations;
            DetectedAppsListBox.ItemsSource = DetectedApps;
            DatabaseComboBox.ItemsSource = AvailableDatabases;

            // Initialize editing integration
            EditingIntegration = new NotionIntegration();

            // Initialize diarization UI state
            RefreshDiarizationModelList();
            UpdateEmbeddingModelStatus();
            DiarizationNumSpeakersBox.Text = AppSettings.Diarization.NumSpeakers.ToString();
            DiarizationThresholdBox.Text = AppSettings.Diarization.Threshold.ToString("F2");
            DiarizationPostMergeCheckBox.IsChecked = AppSettings.Diarization.EnablePostMerge;
            DiarizationPostMergeThresholdBox.Text = AppSettings.Diarization.PostMergeThreshold.ToString("F2");

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
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        public bool IsPickerVisible
        {
            get => _isPickerVisible;
            set
            {
                _isPickerVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        public bool IsListVisible => !IsPickerVisible && !IsFormVisible;

        public string FormTitle
        {
            get => _formTitle;
            set
            {
                _formTitle = value;
                OnPropertyChanged();
            }
        }

        public NotionIntegration EditingIntegration
        {
            get => _editingIntegration;
            set
            {
                _editingIntegration = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Integration> Integrations
        {
            get => _integrations;
            set
            {
                _integrations = value;
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

        public string SelectedSettingsPage
        {
            get => _selectedSettingsPage;
            set
            {
                _selectedSettingsPage = value;
                OnPropertyChanged();
            }
        }

        private void OnSidebarItemClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string page)
            {
                SelectedSettingsPage = page;
            }
        }

        private void OnAddWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            // Show the provider picker instead of the form directly
            IsPickerVisible = true;
            IsFormVisible = false;
        }

        private void OnProviderCardClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string providerTag)
            {
                if (providerTag == "Notion")
                {
                    _selectedProviderType = IntegrationProviderType.Notion;
                    FormTitle = "Add Integration";
                    EditingIntegration = new NotionIntegration();
                    IsPickerVisible = false;
                    IsFormVisible = true;
                }
                // Other providers are "Coming soon" — not clickable
            }
        }

        private void OnPickerBackToList(object sender, RoutedEventArgs e)
        {
            IsPickerVisible = false;
            IsFormVisible = false;
        }

        private void OnFormBackToPicker(object sender, RoutedEventArgs e)
        {
            IsFormVisible = false;
            IsPickerVisible = true;
        }

        private void OnEditWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotionIntegration integration)
            {
                FormTitle = "Edit Integration";
                EditingIntegration = new NotionIntegration
                {
                    Id = integration.Id,
                    DisplayName = integration.DisplayName,
                    ApiKey = integration.ApiKey,
                    SelectedDatabase = integration.SelectedDatabase,
                    Databases = integration.Databases
                };

                // Pre-populate the databases list so the user can re-select
                AvailableDatabases.Clear();
                if (integration.Databases != null)
                {
                    foreach (var db in integration.Databases)
                        AvailableDatabases.Add(db);
                }

                IsPickerVisible = false;
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
            if (sender is Button button && button.Tag is NotionIntegration integration)
            {
                // Testing connection
            }
        }

        private void OnDeleteWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Integration integration)
            {
                Integrations.Remove(integration);
                SaveIntegrations(Integrations);
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

            // Create or update Notion integration
            var integration = new NotionIntegration
            {
                Id = EditingIntegration?.Id ?? Guid.NewGuid().ToString("N")[..16],
                DisplayName = WorkspaceNameBox.Text,
                ApiKey = ApiKeyBox.Password,
                SelectedDatabase = DatabaseComboBox.SelectedItem as NotionDatabase,
                Databases = new ObservableCollection<NotionDatabase>(AvailableDatabases),
                StatusText = "Connected",
                StatusColor = Brushes.Green
            };

            // Add or update in collection
            if (FormTitle == "Add Integration")
            {
                Integrations.Add(integration);
            }
            else
            {
                // Find and update existing integration by ID
                var existingIndex = Integrations.ToList().FindIndex(i => i.Id == EditingIntegration?.Id);
                if (existingIndex >= 0)
                {
                    Integrations[existingIndex] = integration;
                }
            }

            // Save to persistent storage
            SaveIntegrations(Integrations);

            // Clear the form
            WorkspaceNameBox.Text = "";
            ApiKeyBox.Password = "";
            AvailableDatabases.Clear();

            IsFormVisible = false;
            IsPickerVisible = false;
        }

        private void OnCancelWorkspaceClicked(object sender, RoutedEventArgs e)
        {
            IsFormVisible = false;
            IsPickerVisible = false;
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

            AppSettings.Diarization.EnablePostMerge = DiarizationPostMergeCheckBox.IsChecked == true;

            if (float.TryParse(DiarizationPostMergeThresholdBox.Text, out var postMergeThreshold))
                AppSettings.Diarization.PostMergeThreshold = Math.Clamp(postMergeThreshold, 0.5f, 1.0f);

            AppSettings.SaveSettings();
            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string GetIntegrationsFilePath()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingNotesApp");
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, "integrations.json");
        }

        public static void SaveIntegrations(ObservableCollection<Integration> integrations)
        {
            try
            {
                var serializable = integrations.Select(i =>
                {
                    var si = new SerializableIntegration
                    {
                        ProviderType = i.ProviderType.ToString(),
                        Id = i.Id,
                        DisplayName = i.DisplayName,
                        StatusText = i.StatusText
                    };

                    if (i is NotionIntegration notion)
                    {
                        si.ApiKey = notion.ApiKey;
                        si.SelectedDatabase = notion.SelectedDatabase;
                        si.Databases = notion.Databases?.ToList() ?? new List<NotionDatabase>();
                    }
                    else if (i is CsvExportIntegration csv)
                    {
                        si.ExportPath = csv.ExportFolderPath;
                    }
                    else if (i is ExcelExportIntegration excel)
                    {
                        si.ExportPath = excel.ExportPath;
                        si.AppendToSingleFile = excel.AppendToSingleFile;
                    }

                    return si;
                }).ToList();

                var json = JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetIntegrationsFilePath(), json);
            }
            catch (Exception)
            {
                // Failed to save integrations
            }
        }

        public static ObservableCollection<Integration> LoadIntegrations()
        {
            var integrationsPath = GetIntegrationsFilePath();

            // If integrations.json exists, load from it
            if (File.Exists(integrationsPath))
            {
                return LoadIntegrationsFromFile(integrationsPath);
            }

            // Auto-migrate from workspaces.json if it exists
            var workspacesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingNotesApp", "workspaces.json");
            if (File.Exists(workspacesPath))
            {
                return MigrateFromWorkspaces(workspacesPath);
            }

            return new ObservableCollection<Integration>();
        }

        private static ObservableCollection<Integration> LoadIntegrationsFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var serialized = JsonSerializer.Deserialize<List<SerializableIntegration>>(json);

                if (serialized == null)
                    return new ObservableCollection<Integration>();

                var integrations = new ObservableCollection<Integration>();
                foreach (var si in serialized)
                {
                    Integration? integration = si.ProviderType switch
                    {
                        "Notion" => new NotionIntegration
                        {
                            Id = si.Id,
                            DisplayName = si.DisplayName,
                            StatusText = si.StatusText ?? "Connected",
                            StatusColor = Brushes.Green,
                            ApiKey = si.ApiKey ?? "",
                            SelectedDatabase = si.SelectedDatabase,
                            Databases = new ObservableCollection<NotionDatabase>(si.Databases ?? new List<NotionDatabase>())
                        },
                        "CsvExport" => new CsvExportIntegration
                        {
                            Id = si.Id,
                            DisplayName = si.DisplayName,
                            StatusText = si.StatusText ?? "Ready",
                            StatusColor = Brushes.Green,
                            ExportFolderPath = si.ExportPath
                        },
                        "ExcelExport" => new ExcelExportIntegration
                        {
                            Id = si.Id,
                            DisplayName = si.DisplayName,
                            StatusText = si.StatusText ?? "Ready",
                            StatusColor = Brushes.Green,
                            ExportPath = si.ExportPath,
                            AppendToSingleFile = si.AppendToSingleFile ?? false
                        },
                        _ => null
                    };

                    if (integration != null)
                        integrations.Add(integration);
                }

                return integrations;
            }
            catch (Exception)
            {
                return new ObservableCollection<Integration>();
            }
        }

        private static ObservableCollection<Integration> MigrateFromWorkspaces(string workspacesPath)
        {
            try
            {
                var json = File.ReadAllText(workspacesPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                    return new ObservableCollection<Integration>();

                var integrations = new ObservableCollection<Integration>();
                foreach (var sw in root.EnumerateArray())
                {
                    var workspaceName = sw.TryGetProperty("WorkspaceName", out var wn) ? wn.GetString() ?? "" : "";
                    var workspaceId = sw.TryGetProperty("WorkspaceId", out var wi) ? wi.GetString() ?? Guid.NewGuid().ToString("N")[..16] : Guid.NewGuid().ToString("N")[..16];
                    var apiKey = sw.TryGetProperty("ApiKey", out var ak) ? ak.GetString() ?? "" : "";
                    var statusText = sw.TryGetProperty("StatusText", out var st) ? st.GetString() ?? "Connected" : "Connected";

                    NotionDatabase? selectedDb = null;
                    if (sw.TryGetProperty("SelectedDatabase", out var sdProp) && sdProp.ValueKind == JsonValueKind.Object)
                    {
                        selectedDb = new NotionDatabase
                        {
                            Name = sdProp.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "",
                            Id = sdProp.TryGetProperty("Id", out var id) ? id.GetString() ?? "" : "",
                            Type = sdProp.TryGetProperty("Type", out var t) ? t.GetString() ?? "Database" : "Database"
                        };
                    }

                    var databases = new ObservableCollection<NotionDatabase>();
                    if (sw.TryGetProperty("Databases", out var dbArray) && dbArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var dbEl in dbArray.EnumerateArray())
                        {
                            databases.Add(new NotionDatabase
                            {
                                Name = dbEl.TryGetProperty("Name", out var dn) ? dn.GetString() ?? "" : "",
                                Id = dbEl.TryGetProperty("Id", out var di) ? di.GetString() ?? "" : "",
                                Type = dbEl.TryGetProperty("Type", out var dt) ? dt.GetString() ?? "Database" : "Database"
                            });
                        }
                    }

                    integrations.Add(new NotionIntegration
                    {
                        Id = workspaceId,
                        DisplayName = workspaceName,
                        ApiKey = apiKey,
                        SelectedDatabase = selectedDb,
                        Databases = databases,
                        StatusText = statusText,
                        StatusColor = Brushes.Green
                    });
                }

                // Save in new format (auto-migration)
                SaveIntegrations(integrations);

                // Keep old file as backup (per CLAUDE.md edge case)
                return integrations;
            }
            catch (Exception)
            {
                return new ObservableCollection<Integration>();
            }
        }
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
        public float Threshold { get; set; } = 0.85f;      // Clustering threshold (higher = fewer speakers, recommended 0.80-0.90)
        public float MinDurationOn { get; set; } = 0.5f;   // Minimum speech segment duration in seconds
        public float MinDurationOff { get; set; } = 0.8f;  // Minimum silence gap between segments in seconds
        public DiarizationSegmentationModelType SelectedSegmentationModel { get; set; } = DiarizationSegmentationModelType.Pyannote3;
        public bool EnablePostMerge { get; set; } = true;   // Merge similar-sounding speakers after diarization
        public float PostMergeThreshold { get; set; } = 0.75f; // Cosine similarity threshold for merging (0.5-1.0)
    }

    public class ASRSettings
    {
        public ASRModelType SelectedModel { get; set; } = ASRModelType.MoonshineTiny;
    }

    /// <summary>
    /// IMultiValueConverter that returns true when all bound values are equal.
    /// Used by SidebarButtonStyle to compare button Tag with SelectedSettingsPage.
    /// </summary>
    public class EqualityConverter : IMultiValueConverter
    {
        public static readonly EqualityConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;
            return values[0].ToString() == values[1].ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
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
