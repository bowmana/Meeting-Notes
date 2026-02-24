using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Win32;

namespace MeetingNotesApp
{
    public partial class LLamaSharpDebugWindow : Window
    {
        private static readonly string DefaultModelDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingNotesApp", "models");

        private static readonly string DefaultModelPath = Path.Combine(
            DefaultModelDir, "Phi-4-mini-instruct-Q4_K_M.gguf");

        private const string ModelDownloadUrl =
            "https://huggingface.co/unsloth/Phi-4-mini-instruct-GGUF/resolve/main/Phi-4-mini-instruct-Q4_K_M.gguf";

        private const long ExpectedModelSizeBytes = 2_670_000_000;

        private LLamaWeights? _model;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource? _downloadCts;
        private HttpClient? _downloadHttpClient;
        private List<NotionWorkspaceIntegration>? _notionWorkspaces;

        public LLamaSharpDebugWindow()
        {
            InitializeComponent();
            CheckForDefaultModel();
            LoadNotionWorkspaces();
        }

        private void CheckForDefaultModel()
        {
            if (File.Exists(DefaultModelPath))
            {
                ModelPathTextBox.Text = DefaultModelPath;
                ModelStatusText.Text = "Default model found. Click \"Load Model\" to begin.";
                DownloadBanner.Visibility = Visibility.Collapsed;
            }
            else
            {
                ModelPathTextBox.Text = "No model selected";
                ModelStatusText.Text = "No model available. Download or browse for a .gguf model file.";
                LoadModelButton.IsEnabled = false;
                ShowDownloadBanner_NotFound();
            }
        }

        private void ShowDownloadBanner_NotFound()
        {
            DownloadBanner.Visibility = Visibility.Visible;
            BannerAccentBar.Background = (SolidColorBrush)FindResource("InfoBrush");
            BannerHeadline.Text = "AI model not found";
            BannerBody.Text = "To use local AI features, you need the Phi-4 Mini model (2.5 GB download). This is a one-time setup that stores the model on your computer.";
            DownloadProgressPanel.Visibility = Visibility.Collapsed;
            DownloadModelButton.Content = "Download Model";
            DownloadModelButton.Background = (SolidColorBrush)FindResource("PrimaryBrush");
            DownloadModelButton.Visibility = Visibility.Visible;
            CancelDownloadButton.Visibility = Visibility.Collapsed;
            BannerHelperText.Text = "Or use Browse to select your own .gguf model file.";
            BannerHelperText.Visibility = Visibility.Visible;
            BrowseButton.IsEnabled = true;
        }

        private void ShowDownloadBanner_Downloading()
        {
            DownloadBanner.Visibility = Visibility.Visible;
            BannerAccentBar.Background = (SolidColorBrush)FindResource("WarningBrush");
            BannerHeadline.Text = "Downloading AI model...";
            BannerBody.Text = "Downloading from Hugging Face (huggingface.co). You can continue using other features while this downloads.";
            DownloadProgressPanel.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;
            DownloadProgressText.Text = "Starting download...";
            DownloadModelButton.Visibility = Visibility.Collapsed;
            CancelDownloadButton.Visibility = Visibility.Visible;
            BannerHelperText.Visibility = Visibility.Collapsed;
            BrowseButton.IsEnabled = false;
            LoadModelButton.IsEnabled = false;
        }

        private void ShowDownloadBanner_Complete()
        {
            DownloadBanner.Visibility = Visibility.Visible;
            BannerAccentBar.Background = (SolidColorBrush)FindResource("SuccessBrush");
            BannerHeadline.Text = "Download complete!";
            BannerBody.Text = "The AI model has been saved and is ready to use. Click \"Load Model\" to get started.";
            DownloadProgressPanel.Visibility = Visibility.Collapsed;
            DownloadModelButton.Visibility = Visibility.Collapsed;
            CancelDownloadButton.Visibility = Visibility.Collapsed;
            BannerHelperText.Visibility = Visibility.Collapsed;
            BrowseButton.IsEnabled = true;
            LoadModelButton.IsEnabled = true;
            ModelPathTextBox.Text = DefaultModelPath;
            ModelStatusText.Text = "Model downloaded successfully. Click \"Load Model\" to begin.";
        }

        private void ShowDownloadBanner_Failed(string errorMessage)
        {
            DownloadBanner.Visibility = Visibility.Visible;
            BannerAccentBar.Background = (SolidColorBrush)FindResource("ErrorBrush");
            BannerHeadline.Text = "Download failed";
            BannerBody.Text = $"Could not download the AI model. {errorMessage}";
            DownloadProgressPanel.Visibility = Visibility.Collapsed;
            DownloadModelButton.Content = "Try Again";
            DownloadModelButton.Background = (SolidColorBrush)FindResource("PrimaryBrush");
            DownloadModelButton.Visibility = Visibility.Visible;
            CancelDownloadButton.Visibility = Visibility.Collapsed;
            BannerHelperText.Text = "If this keeps happening, you can download the model manually and use Browse to select it.";
            BannerHelperText.Visibility = Visibility.Visible;
            BrowseButton.IsEnabled = true;
            LoadModelButton.IsEnabled = false;
            ModelPathTextBox.Text = "No model selected";
        }

        private void OnBrowseModelClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select GGUF Model File",
                Filter = "GGUF Models (*.gguf)|*.gguf|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ModelPathTextBox.Text = dialog.FileName;
                DownloadBanner.Visibility = Visibility.Collapsed;
                LoadModelButton.IsEnabled = true;
                ModelStatusText.Text = "Model selected. Click \"Load Model\" to begin.";
            }
        }

        private async void OnLoadModelClicked(object sender, RoutedEventArgs e)
        {
            var modelPath = ModelPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(modelPath) || modelPath == "No model selected")
            {
                ModelStatusText.Text = "Please select a model file first.";
                return;
            }

            if (!System.IO.File.Exists(modelPath))
            {
                ModelStatusText.Text = "Model file not found.";
                return;
            }

            // Parse parameters
            if (!uint.TryParse(ContextSizeTextBox.Text, out var contextSize))
                contextSize = 1024;
            if (!int.TryParse(GpuLayerCountTextBox.Text, out var gpuLayerCount))
                gpuLayerCount = -1;

            // Disable UI during loading
            LoadModelButton.IsEnabled = false;
            LoadModelButton.Content = "Loading...";
            ModelStatusText.Text = "Loading model... This may take a moment.";

            var sw = Stopwatch.StartNew();

            try
            {
                // Dispose previous model if any
                _model?.Dispose();
                _model = null;

                var modelParams = new ModelParams(modelPath)
                {
                    ContextSize = contextSize,
                    GpuLayerCount = gpuLayerCount
                };

                _model = await Task.Run(() => LLamaWeights.LoadFromFile(modelParams));

                sw.Stop();
                ModelStatusText.Text = $"Model loaded in {sw.Elapsed.TotalSeconds:F1}s — Context: {contextSize}, GPU layers: {gpuLayerCount}";
                RunInferenceButton.IsEnabled = true;
                UnloadModelButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                ModelStatusText.Text = $"Failed to load model: {ex.Message}";
                _model = null;
            }
            finally
            {
                LoadModelButton.IsEnabled = true;
                LoadModelButton.Content = "Load Model";
            }
        }

        private void OnUnloadModelClicked(object sender, RoutedEventArgs e)
        {
            _model?.Dispose();
            _model = null;
            RunInferenceButton.IsEnabled = false;
            UnloadModelButton.IsEnabled = false;
            ModelStatusText.Text = "Model unloaded.";
        }

        private async void OnRunInferenceClicked(object sender, RoutedEventArgs e)
        {
            if (_model == null)
            {
                InferenceStatsText.Text = "No model loaded.";
                return;
            }

            var prompt = PromptTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                InferenceStatsText.Text = "Please enter a prompt.";
                return;
            }

            // Parse inference parameters
            if (!float.TryParse(TemperatureTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
                temperature = 0.3f;
            if (!int.TryParse(MaxTokensTextBox.Text, out var maxTokens))
                maxTokens = 256;
            if (!uint.TryParse(SeedTextBox.Text, out var seed))
                seed = 1337;

            var antiPrompts = new List<string>();
            foreach (var ap in AntiPromptsTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                antiPrompts.Add(ap.Trim().Replace("\\n", "\n"));
            }

            // Disable UI during inference
            RunInferenceButton.IsEnabled = false;
            RunInferenceButton.Content = "Running...";
            CancelButton.IsEnabled = true;
            OutputTextBox.Clear();
            InferenceStatsText.Text = "Running inference...";

            _cts = new CancellationTokenSource();
            var sw = Stopwatch.StartNew();
            int tokenCount = 0;

            try
            {
                // Parse context size for executor
                if (!uint.TryParse(ContextSizeTextBox.Text, out var contextSize))
                    contextSize = 1024;

                var executorParams = new ModelParams(ModelPathTextBox.Text)
                {
                    ContextSize = contextSize
                };

                var executor = new StatelessExecutor(_model, executorParams);

                // In LLamaSharp 0.26.0, Temperature and Seed are on DefaultSamplingPipeline
                var samplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = temperature,
                    Seed = seed
                };

                var inferenceParams = new InferenceParams
                {
                    MaxTokens = maxTokens,
                    AntiPrompts = antiPrompts,
                    SamplingPipeline = samplingPipeline
                };

                await foreach (var token in executor.InferAsync(prompt, inferenceParams, _cts.Token))
                {
                    tokenCount++;
                    OutputTextBox.AppendText(token);
                    OutputTextBox.ScrollToEnd();

                    // Update stats periodically
                    if (tokenCount % 5 == 0)
                    {
                        var elapsed = sw.Elapsed.TotalSeconds;
                        var tokPerSec = elapsed > 0 ? tokenCount / elapsed : 0;
                        InferenceStatsText.Text = $"Tokens: {tokenCount} | Elapsed: {elapsed:F1}s | {tokPerSec:F1} tok/s";
                    }
                }

                sw.Stop();
                var finalElapsed = sw.Elapsed.TotalSeconds;
                var finalTokPerSec = finalElapsed > 0 ? tokenCount / finalElapsed : 0;
                InferenceStatsText.Text = $"Done — Tokens: {tokenCount} | Elapsed: {finalElapsed:F1}s | {finalTokPerSec:F1} tok/s";
                UpdateSyncButtonState();
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                InferenceStatsText.Text = $"Cancelled — Tokens: {tokenCount} | Elapsed: {sw.Elapsed.TotalSeconds:F1}s";
                UpdateSyncButtonState();
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (ex.Message.Contains("NoKvSlot", StringComparison.OrdinalIgnoreCase))
                {
                    InferenceStatsText.Text = $"Error: Input too long for current context size. " +
                        $"Unload the model, increase Context Size (current: {ContextSizeTextBox.Text}), and reload.";
                }
                else
                {
                    InferenceStatsText.Text = $"Error: {ex.Message}";
                }
            }
            finally
            {
                RunInferenceButton.IsEnabled = _model != null;
                RunInferenceButton.Content = "Run Inference";
                CancelButton.IsEnabled = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void OnDownloadModelClicked(object sender, RoutedEventArgs e)
        {
            ShowDownloadBanner_Downloading();

            _downloadCts = new CancellationTokenSource();
            var token = _downloadCts.Token;
            var tmpPath = DefaultModelPath + ".tmp";

            try
            {
                Directory.CreateDirectory(DefaultModelDir);

                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);

                _downloadHttpClient = new HttpClient();
                _downloadHttpClient.Timeout = TimeSpan.FromHours(2);

                using var response = await _downloadHttpClient.GetAsync(ModelDownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? ExpectedModelSizeBytes;
                long downloadedBytes = 0;
                var startTime = DateTime.UtcNow;

                using var contentStream = await response.Content.ReadAsStreamAsync(token);
                using var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920);

                var buffer = new byte[81920];
                int bytesRead;
                var lastUiUpdate = DateTime.UtcNow;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    downloadedBytes += bytesRead;

                    // Throttle UI updates to ~10 per second
                    var now = DateTime.UtcNow;
                    if ((now - lastUiUpdate).TotalMilliseconds >= 100)
                    {
                        lastUiUpdate = now;
                        var elapsed = (now - startTime).TotalSeconds;
                        var percent = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
                        var downloadedGB = downloadedBytes / (1024.0 * 1024.0 * 1024.0);
                        var totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);

                        var bytesPerSecond = elapsed > 0 ? downloadedBytes / elapsed : 0;
                        var remainingBytes = totalBytes - downloadedBytes;
                        var remainingSeconds = bytesPerSecond > 0 ? remainingBytes / bytesPerSecond : 0;
                        var remainingText = FormatTimeRemaining(remainingSeconds);

                        DownloadProgressBar.Value = percent;
                        DownloadProgressText.Text = $"Downloading... {percent:F0}% ({downloadedGB:F2} GB / {totalGB:F2} GB) — {remainingText}";
                    }
                }

                fileStream.Close();
                if (File.Exists(DefaultModelPath))
                    File.Delete(DefaultModelPath);
                File.Move(tmpPath, DefaultModelPath);

                ShowDownloadBanner_Complete();
            }
            catch (OperationCanceledException)
            {
                CleanupTmpFile(tmpPath);
                ShowDownloadBanner_NotFound();
                ModelStatusText.Text = "Download cancelled.";
            }
            catch (Exception ex)
            {
                CleanupTmpFile(tmpPath);
                ShowDownloadBanner_Failed(ex.Message);
            }
            finally
            {
                _downloadHttpClient?.Dispose();
                _downloadHttpClient = null;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        private void OnCancelDownloadClicked(object sender, RoutedEventArgs e)
        {
            _downloadCts?.Cancel();
        }

        private static string FormatTimeRemaining(double seconds)
        {
            if (seconds <= 0 || double.IsInfinity(seconds) || double.IsNaN(seconds))
                return "Estimating time remaining...";
            if (seconds < 60)
                return $"About {seconds:F0} seconds remaining";
            if (seconds < 3600)
                return $"About {seconds / 60:F0} minutes remaining";
            return $"About {seconds / 3600:F1} hours remaining";
        }

        private static void CleanupTmpFile(string tmpPath)
        {
            try
            {
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void OnClearOutputClicked(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
            InferenceStatsText.Text = "";
        }

        // --- Notion Sync ---

        private void LoadNotionWorkspaces()
        {
            var allWorkspaces = SettingsWindow.LoadWorkspaces();
            _notionWorkspaces = allWorkspaces
                .Where(w => w.SelectedDatabase != null && !string.IsNullOrEmpty(w.ApiKey))
                .ToList();

            if (_notionWorkspaces.Count == 0)
            {
                NotionSyncPanel.Visibility = Visibility.Collapsed;
                return;
            }

            NotionSyncPanel.Visibility = Visibility.Visible;
            NotionWorkspaceComboBox.ItemsSource = _notionWorkspaces;
            NotionWorkspaceComboBox.SelectedIndex = 0;
        }

        private void OnNotionWorkspaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotionWorkspaceComboBox.SelectedItem is NotionWorkspaceIntegration workspace)
            {
                SelectedDatabaseText.Text = workspace.SelectedDatabase?.Name ?? "(none)";
                UpdateSyncButtonState();
            }
        }

        private void UpdateSyncButtonState()
        {
            SyncToNotionButton.IsEnabled =
                !string.IsNullOrWhiteSpace(OutputTextBox.Text) &&
                NotionWorkspaceComboBox.SelectedItem is NotionWorkspaceIntegration ws &&
                ws.SelectedDatabase != null;
        }

        private async void OnSyncToNotionClicked(object sender, RoutedEventArgs e)
        {
            if (NotionWorkspaceComboBox.SelectedItem is not NotionWorkspaceIntegration workspace)
            {
                NotionSyncStatusText.Text = "No workspace selected.";
                return;
            }

            var outputText = OutputTextBox.Text;
            if (string.IsNullOrWhiteSpace(outputText))
            {
                NotionSyncStatusText.Text = "No output to sync.";
                return;
            }

            SyncToNotionButton.IsEnabled = false;
            SyncToNotionButton.Content = "Syncing...";
            NotionSyncStatusText.Text = "Creating Notion page...";
            NotionSyncStatusText.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");

            try
            {
                await CreateNotionPageWithBlocks(workspace, outputText);
                NotionSyncStatusText.Text = "Synced successfully!";
                NotionSyncStatusText.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            catch (Exception ex)
            {
                NotionSyncStatusText.Text = $"Sync failed: {ex.Message}";
                NotionSyncStatusText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
            finally
            {
                SyncToNotionButton.IsEnabled = true;
                SyncToNotionButton.Content = "Sync to Notion";
            }
        }

        private async Task CreateNotionPageWithBlocks(NotionWorkspaceIntegration workspace, string outputText)
        {
            var database = workspace.SelectedDatabase!;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workspace.ApiKey);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            // Query database schema to find the title property name
            var titlePropertyName = await GetTitlePropertyName(httpClient, database.Id);

            var sections = ParseStructuredOutput(outputText);
            var blockChildren = BuildNotionBlocks(sections);
            var pageTitle = $"LLamaSharp Debug - {DateTime.Now:yyyy-MM-dd HH:mm}";

            var properties = new Dictionary<string, object>
            {
                [titlePropertyName] = new
                {
                    title = new[] { new { text = new { content = pageTitle } } }
                }
            };

            var requestBody = new Dictionary<string, object>
            {
                ["parent"] = new { database_id = database.Id },
                ["properties"] = properties,
                ["children"] = blockChildren
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.notion.com/v1/pages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Notion API {response.StatusCode}: {errorContent}");
            }
        }

        private static async Task<string> GetTitlePropertyName(HttpClient httpClient, string databaseId)
        {
            var response = await httpClient.GetAsync($"https://api.notion.com/v1/databases/{databaseId}");
            if (!response.IsSuccessStatusCode)
                return "Name"; // Default fallback

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("properties", out var properties))
            {
                foreach (var prop in properties.EnumerateObject())
                {
                    if (prop.Value.TryGetProperty("type", out var type) &&
                        type.GetString() == "title")
                    {
                        return prop.Name;
                    }
                }
            }

            return "Name";
        }

        // --- Output Parsing ---

        private class ParsedMeetingOutput
        {
            public string Summary { get; set; } = "";
            public List<string> KeyPoints { get; set; } = new();
            public List<string> ActionItems { get; set; } = new();
            public string RawText { get; set; } = "";
        }

        private static ParsedMeetingOutput ParseStructuredOutput(string text)
        {
            var result = new ParsedMeetingOutput();
            var lines = text.Split('\n');
            string currentSection = "none";

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "summary";
                    var remainder = line.Substring("SUMMARY:".Length).Trim();
                    if (!string.IsNullOrEmpty(remainder))
                        result.Summary += remainder + " ";
                    continue;
                }
                if (line.StartsWith("KEY POINTS:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("KEY_POINTS:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "keypoints";
                    continue;
                }
                if (line.StartsWith("ACTION ITEMS:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("ACTION_ITEMS:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "actionitems";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                switch (currentSection)
                {
                    case "summary":
                        result.Summary += line + " ";
                        break;
                    case "keypoints":
                        var kpText = line.TrimStart('-', '*', ' ');
                        if (!string.IsNullOrEmpty(kpText) && !kpText.Equals("None", StringComparison.OrdinalIgnoreCase))
                            result.KeyPoints.Add(kpText);
                        break;
                    case "actionitems":
                        var aiText = line.TrimStart('-', ' ');
                        if (aiText.StartsWith("[ ]") || aiText.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
                            aiText = aiText.Substring(3).Trim();
                        if (!string.IsNullOrEmpty(aiText) && !aiText.Equals("None", StringComparison.OrdinalIgnoreCase))
                            result.ActionItems.Add(aiText);
                        break;
                    default:
                        result.RawText += line + "\n";
                        break;
                }
            }

            result.Summary = result.Summary.Trim();
            result.RawText = result.RawText.Trim();
            return result;
        }

        // --- Notion Block Builders ---

        private static List<object> BuildNotionBlocks(ParsedMeetingOutput sections)
        {
            var blocks = new List<object>();
            bool hasStructured = !string.IsNullOrEmpty(sections.Summary) ||
                                 sections.KeyPoints.Count > 0 ||
                                 sections.ActionItems.Count > 0;

            if (hasStructured)
            {
                if (!string.IsNullOrEmpty(sections.Summary))
                {
                    blocks.Add(MakeHeading2Block("Summary"));
                    blocks.AddRange(MakeParagraphBlocks(sections.Summary));
                }

                if (sections.KeyPoints.Count > 0)
                {
                    blocks.Add(MakeHeading2Block("Key Points"));
                    foreach (var point in sections.KeyPoints)
                        blocks.Add(MakeBulletedListItemBlock(point));
                }

                if (sections.ActionItems.Count > 0)
                {
                    blocks.Add(MakeHeading2Block("Action Items"));
                    foreach (var item in sections.ActionItems)
                        blocks.Add(MakeToDoBlock(item));
                }
            }

            if (!string.IsNullOrEmpty(sections.RawText))
            {
                if (hasStructured)
                    blocks.Add(MakeHeading2Block("Additional Output"));
                blocks.AddRange(MakeParagraphBlocks(sections.RawText));
            }

            if (blocks.Count == 0)
                blocks.AddRange(MakeParagraphBlocks("(No output content)"));

            return blocks;
        }

        private static object MakeHeading2Block(string text)
        {
            return new Dictionary<string, object>
            {
                ["object"] = "block",
                ["type"] = "heading_2",
                ["heading_2"] = new
                {
                    rich_text = new[] { new { type = "text", text = new { content = text } } }
                }
            };
        }

        private static object MakeBulletedListItemBlock(string text)
        {
            var truncated = text.Length > 2000 ? text.Substring(0, 2000) : text;
            return new Dictionary<string, object>
            {
                ["object"] = "block",
                ["type"] = "bulleted_list_item",
                ["bulleted_list_item"] = new
                {
                    rich_text = new[] { new { type = "text", text = new { content = truncated } } }
                }
            };
        }

        private static object MakeToDoBlock(string text)
        {
            var truncated = text.Length > 2000 ? text.Substring(0, 2000) : text;
            return new Dictionary<string, object>
            {
                ["object"] = "block",
                ["type"] = "to_do",
                ["to_do"] = new
                {
                    rich_text = new[] { new { type = "text", text = new { content = truncated } } },
                    @checked = false
                }
            };
        }

        private static List<object> MakeParagraphBlocks(string text)
        {
            var blocks = new List<object>();
            const int maxChars = 2000;

            if (text.Length <= maxChars)
            {
                blocks.Add(new Dictionary<string, object>
                {
                    ["object"] = "block",
                    ["type"] = "paragraph",
                    ["paragraph"] = new
                    {
                        rich_text = new[] { new { type = "text", text = new { content = text } } }
                    }
                });
                return blocks;
            }

            int offset = 0;
            while (offset < text.Length)
            {
                int remaining = text.Length - offset;
                int chunkSize = Math.Min(remaining, maxChars);

                if (offset + chunkSize < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', offset + chunkSize - 1, chunkSize);
                    if (lastSpace > offset)
                        chunkSize = lastSpace - offset;
                }

                var chunk = text.Substring(offset, chunkSize).Trim();
                if (!string.IsNullOrEmpty(chunk))
                {
                    blocks.Add(new Dictionary<string, object>
                    {
                        ["object"] = "block",
                        ["type"] = "paragraph",
                        ["paragraph"] = new
                        {
                            rich_text = new[] { new { type = "text", text = new { content = chunk } } }
                        }
                    });
                }

                offset += chunkSize;
                if (offset < text.Length && text[offset] == ' ')
                    offset++;
            }

            return blocks;
        }

        protected override void OnClosed(EventArgs e)
        {
            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
            _downloadHttpClient?.Dispose();

            _cts?.Cancel();
            _cts?.Dispose();

            _model?.Dispose();
            _model = null;
            base.OnClosed(e);
        }
    }
}
