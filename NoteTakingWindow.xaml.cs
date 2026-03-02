using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using MeetingNotesApp.Models;
using MeetingNotesApp.Services;

namespace MeetingNotesApp
{
    public partial class NoteTakingWindow : Window, INotifyPropertyChanged
    {
        private string _meetingTitle = "Team Standup";
        private string _meetingPlatform = "Microsoft Teams";
        private string _meetingOrganizer = "";
        private string _meetingAttendees = "";
        private string _databaseInfo = "Unknown Database";
        private string _meetingStartTime = DateTime.Now.ToString("MM/dd/yy HH:mm");
        private string _meetingDuration = "00:05:30";
        private string _liveTranscription = "";
        private string _manualNotes = "";
        private string _aiSummary = "";
        private ObservableCollection<KeyPoint> _keyPoints;
        private ObservableCollection<ActionItem> _actionItems;
        private DispatcherTimer _timer;
        private MeetingInfo _meetingInfo;
        
        // Transcription-related fields
        private ISpeechRecognitionService _asrService;
        private WasapiLoopbackCapture _loopbackCapture;
        private WaveFileWriter _waveFileWriter;
        private MemoryStream _audioStream;
        private bool _isRecording = false;
        private string _recordingButtonText = "Start Recording";
        private Brush _recordingButtonColor = Brushes.Green;
        private string _transcriptionStatus = "Ready to record";
        private Brush _transcriptionStatusColor = Brushes.Gray;
        private string _saveButtonText = "Save Notes";

        // Speaker diarization fields
        private ISpeakerDiarizationService _diarizationService;
        private DiarizedTranscription _diarizedTranscription;
        private CancellationTokenSource _diarizationCts;
        private string _diarizationStatus = "";
        private double _diarizationProgress = 0;
        private bool _isDiarizing = false;
        private int _detectedSpeakerCount = 0;

        // Speaker identification fields
        private ObservableCollection<SpeakerEntry> _speakerEntries = new();
        private ISpeakerNameInferenceService _nameInferenceService;
        private SpeakerEmbeddingHelper _embeddingHelper;
        private SpeakerProfileService _speakerProfileService;

        // Audio data retained for speaker enrollment after naming
        private float[]? _lastFullAudioSamples;
        private List<(float Start, float End, int Speaker)>? _lastDiarizationSegments;

        public NoteTakingWindow(MeetingInfo meetingInfo = null)
        {
            InitializeComponent();
            DataContext = this;
            
            _meetingInfo = meetingInfo;

            // Initialize collections
            KeyPoints = new ObservableCollection<KeyPoint>
            {
                new KeyPoint { Text = "Discuss project timeline", IsCompleted = false },
                new KeyPoint { Text = "Review bug fixes", IsCompleted = true },
                new KeyPoint { Text = "Plan next sprint", IsCompleted = false }
            };

            ActionItems = new ObservableCollection<ActionItem>
            {
                new ActionItem { Text = "Update documentation", Assignee = "John", IsCompleted = false },
                new ActionItem { Text = "Fix login bug", Assignee = "Sarah", IsCompleted = false },
                new ActionItem { Text = "Review PR #123", Assignee = "Mike", IsCompleted = true }
            };

            // Set up list boxes
            KeyPointsListBox.ItemsSource = KeyPoints;
            ActionItemsListBox.ItemsSource = ActionItems;

            // Initialize audio capture and services
            try
            {
                _loopbackCapture = new WasapiLoopbackCapture();
                _loopbackCapture.DataAvailable += OnSystemAudioDataAvailable;
                _loopbackCapture.RecordingStopped += OnSystemAudioRecordingStopped;
                _audioStream = new MemoryStream();
                TranscriptionStatus = "Ready to capture system audio";
                TranscriptionStatusColor = Brushes.Gray;
            }
            catch (Exception ex)
            {
                TranscriptionStatus = $"Audio capture error: {ex.Message}";
                TranscriptionStatusColor = Brushes.Red;
            }

            AppSettings.LoadSettings();
            _asrService = new SherpaOnnxASRService(AppSettings.ASR.SelectedModel);
            _diarizationService = new SherpaOnnxDiarizationService();
            _nameInferenceService = new SpeakerNameInferenceService();
            _embeddingHelper = new SpeakerEmbeddingHelper();
            _speakerProfileService = new SpeakerProfileService(_embeddingHelper);

            // Initialize meeting info if provided
            if (_meetingInfo != null)
            {
                MeetingTitle = _meetingInfo.Title;
                MeetingStartTime = _meetingInfo.StartTime.ToString("MM/dd/yy HH:mm");
                MeetingPlatform = _meetingInfo.Integration?.DisplayName ?? "Unknown Platform";
                SaveButtonText = _meetingInfo.Integration?.SaveButtonText ?? "Save Notes";

                // Set database info
                var integration = _meetingInfo.Integration;
                if (integration is NotionIntegration notionInt && notionInt.SelectedDatabase != null)
                {
                    var dbName = notionInt.SelectedDatabase.Name;
                    if (dbName.Contains($"({integration.DisplayName})"))
                    {
                        DatabaseInfo = dbName;
                    }
                    else
                    {
                        DatabaseInfo = $"{dbName} ({integration.DisplayName})";
                    }
                }
                else
                {
                    DatabaseInfo = integration?.TargetDescription ?? "No Database Selected";
                }
            }

            // Start timer to update duration
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDuration();
            _timer.Start();
        }

        public string MeetingTitle
        {
            get => _meetingTitle;
            set
            {
                _meetingTitle = value;
                OnPropertyChanged();
            }
        }

        public string MeetingPlatform
        {
            get => _meetingPlatform;
            set
            {
                _meetingPlatform = value;
                OnPropertyChanged();
            }
        }

        public string MeetingOrganizer
        {
            get => _meetingOrganizer;
            set
            {
                _meetingOrganizer = value;
                OnPropertyChanged();
            }
        }

        public string MeetingAttendees
        {
            get => _meetingAttendees;
            set
            {
                _meetingAttendees = value;
                OnPropertyChanged();
            }
        }

        public string DatabaseInfo
        {
            get => _databaseInfo;
            set
            {
                _databaseInfo = value;
                OnPropertyChanged();
            }
        }

        public string MeetingStartTime
        {
            get => _meetingStartTime;
            set
            {
                _meetingStartTime = value;
                OnPropertyChanged();
            }
        }

        public string MeetingDuration
        {
            get => _meetingDuration;
            set
            {
                _meetingDuration = value;
                OnPropertyChanged();
            }
        }

        public string LiveTranscription
        {
            get => _liveTranscription;
            set
            {
                _liveTranscription = value;
                OnPropertyChanged();
            }
        }

        public string TranscriptionStatus
        {
            get => _transcriptionStatus;
            set
            {
                _transcriptionStatus = value;
                OnPropertyChanged();
            }
        }

        public Brush TranscriptionStatusColor
        {
            get => _transcriptionStatusColor;
            set
            {
                _transcriptionStatusColor = value;
                OnPropertyChanged();
            }
        }

        public string ManualNotes
        {
            get => _manualNotes;
            set
            {
                _manualNotes = value;
                OnPropertyChanged();
            }
        }

        public string AiSummary
        {
            get => _aiSummary;
            set
            {
                _aiSummary = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<KeyPoint> KeyPoints
        {
            get => _keyPoints;
            set
            {
                _keyPoints = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ActionItem> ActionItems
        {
            get => _actionItems;
            set
            {
                _actionItems = value;
                OnPropertyChanged();
            }
        }

        public string RecordingButtonText
        {
            get => _recordingButtonText;
            set
            {
                _recordingButtonText = value;
                OnPropertyChanged();
            }
        }

        public Brush RecordingButtonColor
        {
            get => _recordingButtonColor;
            set
            {
                _recordingButtonColor = value;
                OnPropertyChanged();
            }
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            set
            {
                _saveButtonText = value;
                OnPropertyChanged();
            }
        }

        public string DiarizationStatus
        {
            get => _diarizationStatus;
            set
            {
                _diarizationStatus = value;
                OnPropertyChanged();
            }
        }

        public double DiarizationProgress
        {
            get => _diarizationProgress;
            set
            {
                _diarizationProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiarizing
        {
            get => _isDiarizing;
            set
            {
                _isDiarizing = value;
                OnPropertyChanged();
            }
        }

        public int DetectedSpeakerCount
        {
            get => _detectedSpeakerCount;
            set
            {
                _detectedSpeakerCount = value;
                OnPropertyChanged();
            }
        }

        private void UpdateDuration()
        {
            // Simulate duration update - use current time as start time for demo
            var startTime = DateTime.Now.AddMinutes(-5); // Demo: started 5 minutes ago
            var duration = DateTime.Now - startTime;
            MeetingDuration = duration.ToString(@"hh\:mm\:ss");
        }

        private void OnAddKeyPointClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(NewKeyPointBox.Text))
            {
                KeyPoints.Add(new KeyPoint { Text = NewKeyPointBox.Text, IsCompleted = false });
                NewKeyPointBox.Text = "";
            }
        }

        private void OnAddActionItemClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(NewActionItemBox.Text))
            {
                ActionItems.Add(new ActionItem { Text = NewActionItemBox.Text, Assignee = "TBD", IsCompleted = false });
                NewActionItemBox.Text = "";
            }
        }

        private void OnStopRecordingClicked(object sender, RoutedEventArgs e)
            {
                TranscriptionStatus = "Stopped";
                TranscriptionStatusColor = (Brush)FindResource("ErrorBrush");
                _timer.Stop();
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (_meetingInfo?.Integration == null)
            {
                MessageBox.Show("No valid integration configured for saving.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Disable the button to prevent multiple clicks
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "Saving...";

                var meetingData = BuildMeetingData();
                var saveService = GetSaveService(_meetingInfo.Integration.ProviderType);
                await saveService.SaveMeetingAsync(meetingData, _meetingInfo.Integration);

                MessageBox.Show("Meeting notes saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable the button
                var button = sender as Button;
                button.IsEnabled = true;
                button.Content = SaveButtonText;
            }
        }

        private MeetingData BuildMeetingData()
        {
            // Build speakers description from diarization data
            var speakersDescription = "";
            var speakerCount = 0;
            if (_diarizedTranscription != null && _diarizedTranscription.SpeakerCount > 0)
            {
                speakerCount = _diarizedTranscription.SpeakerCount;
                var namedSpeakers = _diarizedTranscription.GetNamedAttendees();
                speakersDescription = !string.IsNullOrWhiteSpace(namedSpeakers)
                    ? $"{speakerCount} speakers: {namedSpeakers}"
                    : $"{speakerCount} speakers detected";
            }

            // Parse start time
            DateTime date = default;
            if (DateTime.TryParse(MeetingStartTime, out var parsedTime))
            {
                date = parsedTime;
            }

            return new MeetingData
            {
                Title = MeetingTitle,
                Transcription = LiveTranscription ?? "",
                ManualNotes = ManualNotes ?? "",
                AiSummary = AiSummary ?? "",
                KeyPoints = KeyPoints.Select(kp => kp.Text).ToList(),
                ActionItems = ActionItems.Select(ai => (ai.Text, ai.Assignee)).ToList(),
                Duration = MeetingDuration,
                Organizer = MeetingOrganizer ?? "",
                Attendees = MeetingAttendees ?? "",
                Date = date,
                SpeakerCount = speakerCount,
                SpeakersDescription = speakersDescription
            };
        }

        private static IMeetingSaveService GetSaveService(IntegrationProviderType providerType)
        {
            return providerType switch
            {
                IntegrationProviderType.Notion => new NotionSaveService(),
                IntegrationProviderType.CsvExport => new CsvSaveService(),
                IntegrationProviderType.ExcelExport => new ExcelSaveService(),
                _ => throw new NotSupportedException($"Save service for {providerType} is not yet implemented.")
            };
        }

        private async void OnGenerateSummaryClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable button during processing
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "Generating...";

                // Generate summary from actual transcription and notes content
                var summary = await GenerateMeetingSummary();
                AiSummary = summary;
            }
            catch (Exception ex)
            {
                // Error generating summary
            }
            finally
            {
                // Re-enable button
                var button = sender as Button;
                button.IsEnabled = true;
                button.Content = "Generate Summary";
            }
        }

        private async Task<string> GenerateMeetingSummary()
        {
            // Combine transcription and notes for AI summarization
            var combinedContent = "";
            
            if (!string.IsNullOrWhiteSpace(LiveTranscription))
            {
                combinedContent += "Meeting Transcription:\n" + LiveTranscription + "\n\n";
            }
            
            if (!string.IsNullOrWhiteSpace(ManualNotes))
            {
                if (!string.IsNullOrWhiteSpace(combinedContent))
                {
                    combinedContent += "Additional Notes:\n" + ManualNotes + "\n";
                }
                else
                {
                    combinedContent += "Meeting Notes:\n" + ManualNotes + "\n";
                }
            }
            
            if (string.IsNullOrWhiteSpace(combinedContent))
            {
                return "No transcription or notes available to summarize.";
            }
            
            // Always generate a summary, even for minimal content
            
            // Use LMStudio to generate structured summary
            try
            {
                var aiSummary = await GenerateOpenAISummary(combinedContent);
                
                // Add user's manual notes at the bottom if they exist
                if (!string.IsNullOrWhiteSpace(ManualNotes))
                {
                    aiSummary += "\n\n### Additional Notes\n" + ManualNotes;
                }
                
                return aiSummary;
            }
            catch (Exception ex)
            {
                // Fallback to simple truncation if AI fails
                var fallbackSummary = SummarizeTextFallback(combinedContent);
                
                // Add user's manual notes at the bottom if they exist
                if (!string.IsNullOrWhiteSpace(ManualNotes))
                {
                    fallbackSummary += "\n\n### Additional Notes\n" + ManualNotes;
                }
                
                return fallbackSummary;
            }
        }

        private async Task<string> GenerateOpenAISummary(string content)
        {
            using var httpClient = new HttpClient();

            var requestBody = new
            {
                model = "meta-llama-3.1-8b-instruct",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"You are a meeting assistant. The transcription may include speaker labels (Speaker 1, Speaker 2, etc.) with timestamps. Use these to attribute statements and action items to specific speakers.

Create a structured meeting summary in the following exact format:

### Meeting Overview
[Brief 2-3 sentence overview including key participants if speaker labels are present]

### Discussion by Topic
[Group related discussion points. If speaker labels are present, note which speaker said what.]

### Action Items
[Extract specific action items in this format:]
- [ ] [Speaker/Person] to [specific action] by [timeframe if mentioned]

### Decisions Made
[List any decisions, noting who proposed/agreed if speaker labels are present. If none, write 'No specific decisions identified.']

IMPORTANT: Always create a summary based on the provided content, no matter how brief or minimal it seems. If speaker labels are present, use them to attribute statements and decisions. If no specific assignee is mentioned, use 'Team' or 'TBD'. Keep the summary professional and structured with proper markdown formatting."
                    },
                    new
                    {
                        role = "user",
                        content = $"Please summarize this meeting content:\n\n{content}"
                    }
                },
                max_tokens = 500,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content_post = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("http://127.0.0.1:1234/v1/chat/completions", content_post);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"LMStudio API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            return responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No summary generated.";
        }

        private string SummarizeTextFallback(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "No content available.";
            
            // Simple fallback summarization logic
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (sentences.Length <= 3)
            {
                return text.Trim();
            }
            
            var summarySentences = sentences.Take(3).ToArray();
            var summary = string.Join(". ", summarySentences).Trim();
            
            if (sentences.Length > 3)
            {
                summary += "...";
            }
            
            return summary;
        }

        private async Task RunSpeakerIdentificationPipelineAsync(
            float[] fullSamples,
            List<(float Start, float End, int Speaker)> rawSegments,
            CancellationToken ct)
        {
            var identifiedIndices = new HashSet<int>();

            // Step 1: Try voice fingerprint matching against stored profiles
            if (_speakerProfileService.IsAvailable && _speakerProfileService.Profiles.Count > 0)
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        SpeakerPanelStatus.Text = "(matching voices against known speakers...)";
                    });

                    var speakerIndices = _diarizedTranscription!.GetSpeakerIndices();
                    foreach (var idx in speakerIndices)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Get segments for this speaker
                        var speakerSegs = rawSegments
                            .Where(s => s.Speaker == idx)
                            .Select(s => (s.Start, s.End))
                            .ToArray();

                        if (speakerSegs.Length == 0) continue;

                        // Extract embedding from combined speaker audio
                        var embedding = await _embeddingHelper.ExtractEmbeddingFromSegmentsAsync(
                            fullSamples, 16000, speakerSegs);

                        if (embedding.Length == 0) continue;

                        // Match against stored profiles
                        var (name, confidence) = _speakerProfileService.IdentifySpeaker(embedding);
                        if (name != null)
                        {
                            identifiedIndices.Add(idx);
                            _diarizedTranscription.SetSpeakerName(idx, name);

                            Dispatcher.Invoke(() =>
                            {
                                var entry = _speakerEntries.FirstOrDefault(e => e.SpeakerIndex == idx);
                                if (entry != null)
                                {
                                    entry.Name = name;
                                    entry.ConfidenceBadge = $"voice match ({confidence:P0})";
                                    entry.ConfidenceColor = confidence >= 0.8f ? Brushes.Green : Brushes.Orange;
                                }
                            });
                        }
                    }

                    if (identifiedIndices.Count > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LiveTranscription = _diarizedTranscription!.ToFlatText();
                            var attendees = _diarizedTranscription.GetNamedAttendees();
                            if (!string.IsNullOrWhiteSpace(attendees))
                                MeetingAttendees = attendees;
                        });
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    // Voice matching failed — continue to LLM inference
                }
            }

            // Step 2: Run LLM inference for any speakers not yet identified by voice
            await RunSpeakerNameInferenceAsync(ct, identifiedIndices);
        }

        private async Task RunSpeakerNameInferenceAsync(CancellationToken ct, HashSet<int>? skipIndices = null)
        {
            if (_diarizedTranscription == null || !_nameInferenceService.IsModelAvailable)
                return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    SpeakerPanelStatus.Text = "(identifying speakers via AI...)";
                });

                var inferences = await _nameInferenceService.InferSpeakerNamesAsync(_diarizedTranscription, ct);

                Dispatcher.Invoke(() =>
                {
                    int applied = 0;
                    foreach (var inference in inferences)
                    {
                        // Skip speakers already identified by voice matching
                        if (skipIndices != null && skipIndices.Contains(inference.SpeakerIndex))
                            continue;

                        // Apply to the data model
                        _diarizedTranscription.SetSpeakerName(inference.SpeakerIndex, inference.InferredName);

                        // Update the speaker panel entry
                        var entry = _speakerEntries.FirstOrDefault(e => e.SpeakerIndex == inference.SpeakerIndex);
                        if (entry != null)
                        {
                            entry.Name = inference.InferredName;

                            if (inference.Confidence >= 0.9f)
                            {
                                entry.ConfidenceBadge = "AI: high confidence";
                                entry.ConfidenceColor = Brushes.Green;
                            }
                            else if (inference.Confidence >= 0.8f)
                            {
                                entry.ConfidenceBadge = "AI: medium confidence";
                                entry.ConfidenceColor = Brushes.Orange;
                            }
                            else
                            {
                                entry.ConfidenceBadge = "AI: low confidence";
                                entry.ConfidenceColor = Brushes.Gray;
                            }

                            applied++;
                        }
                    }

                    // Refresh transcript with inferred names
                    LiveTranscription = _diarizedTranscription.ToFlatText();

                    // Auto-populate attendees
                    var attendees = _diarizedTranscription.GetNamedAttendees();
                    if (!string.IsNullOrWhiteSpace(attendees))
                        MeetingAttendees = attendees;

                    var voiceMatched = skipIndices?.Count ?? 0;
                    var total = _speakerEntries.Count;
                    var totalIdentified = voiceMatched + applied;
                    SpeakerPanelStatus.Text = totalIdentified > 0
                        ? $"(identified {totalIdentified}/{total} speakers — edit to correct)"
                        : $"({total} speakers detected — type names to identify)";
                });
            }
            catch (OperationCanceledException)
            {
                // Inference was cancelled — no update needed
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    SpeakerPanelStatus.Text = $"(AI inference failed: {ex.Message})";
                });
            }
        }

        private void PopulateSpeakerPanel()
        {
            if (_diarizedTranscription == null || _diarizedTranscription.SpeakerCount == 0)
                return;

            _speakerEntries.Clear();

            var speakerIndices = _diarizedTranscription.GetSpeakerIndices();
            foreach (var idx in speakerIndices)
            {
                var existingName = _diarizedTranscription.SpeakerNames.GetValueOrDefault(idx);
                _speakerEntries.Add(new SpeakerEntry
                {
                    SpeakerIndex = idx,
                    DefaultLabel = $"Speaker {idx + 1}",
                    Name = existingName ?? ""
                });
            }

            SpeakerItemsControl.ItemsSource = _speakerEntries;
            SpeakerPanelBorder.Visibility = Visibility.Visible;
            SpeakerPanelStatus.Text = $"({speakerIndices.Count} speakers detected — type names to identify)";
        }

        private void ApplySpeakerName(SpeakerEntry entry)
        {
            if (_diarizedTranscription == null) return;

            var name = entry.Name?.Trim();
            _diarizedTranscription.SetSpeakerName(entry.SpeakerIndex, name);

            // Refresh transcript display
            LiveTranscription = _diarizedTranscription.ToFlatText();

            // Auto-populate attendees from named speakers
            var attendees = _diarizedTranscription.GetNamedAttendees();
            if (!string.IsNullOrWhiteSpace(attendees))
            {
                MeetingAttendees = attendees;
            }

            // Enroll speaker voice profile for future meeting identification
            if (!string.IsNullOrWhiteSpace(name) && _lastFullAudioSamples != null && _lastDiarizationSegments != null)
            {
                _ = EnrollSpeakerAsync(entry.SpeakerIndex, name);
            }
        }

        private async Task EnrollSpeakerAsync(int speakerIndex, string name)
        {
            if (!_embeddingHelper.IsModelAvailable || _lastFullAudioSamples == null || _lastDiarizationSegments == null)
                return;

            try
            {
                var speakerSegs = _lastDiarizationSegments
                    .Where(s => s.Speaker == speakerIndex)
                    .Select(s => (s.Start, s.End))
                    .ToArray();

                if (speakerSegs.Length == 0) return;

                var embedding = await _embeddingHelper.ExtractEmbeddingFromSegmentsAsync(
                    _lastFullAudioSamples, 16000, speakerSegs);

                if (embedding.Length > 0)
                {
                    _speakerProfileService.EnrollWithEmbedding(name, embedding);
                }
            }
            catch
            {
                // Best effort — don't block the user if enrollment fails
            }
        }

        private void OnSpeakerNameChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is SpeakerEntry entry)
            {
                ApplySpeakerName(entry);
            }
        }

        private void OnSpeakerNameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && sender is TextBox textBox && textBox.DataContext is SpeakerEntry entry)
            {
                ApplySpeakerName(entry);
                // Move focus to next text box
                textBox.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            // Cancel any in-progress diarization
            _diarizationCts?.Cancel();
            // Clean up transcription resources
            StopRecording();
            _asrService?.Dispose();
            _loopbackCapture?.Dispose();
            _waveFileWriter?.Dispose();
            _audioStream?.Dispose();
            _diarizationService?.Dispose();
            _nameInferenceService?.Dispose();
            _speakerProfileService?.Dispose();
            _embeddingHelper?.Dispose();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSystemAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            // Write audio data to memory stream
            if (_audioStream != null && e.Buffer != null)
            {
                _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void OnSystemAudioRecordingStopped(object sender, StoppedEventArgs e)
        {
            // Process the captured audio for speech recognition
            if (_audioStream != null && _audioStream.Length > 0)
            {
                ProcessCapturedAudio();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = "No audio captured - check microphone permissions";
                    TranscriptionStatusColor = Brushes.Orange;
                });
            }
        }

        private async void ProcessCapturedAudio()
        {
            try
            {
                // Reset stream position
                _audioStream.Position = 0;

                // Check if we actually captured any audio
                if (_audioStream.Length == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TranscriptionStatus = "No audio captured - check if meeting audio is playing";
                        TranscriptionStatusColor = Brushes.Orange;
                    });
                    return;
                }

                // Create a temporary WAV file from the captured audio
                var tempFile = Path.GetTempFileName() + ".wav";

                // Write WAV header and audio data
                using (var writer = new WaveFileWriter(tempFile, _loopbackCapture.WaveFormat))
                {
                    _audioStream.Position = 0;
                    _audioStream.CopyTo(writer);
                }

                var fileSize = new FileInfo(tempFile).Length;
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = $"Processing audio ({fileSize / 1024} KB)...";
                    TranscriptionStatusColor = Brushes.Blue;
                });

                // Convert audio format to 16kHz mono WAV (used for both diarization and speech recognition)
                var convertedFile = ConvertAudioForSpeechRecognition(tempFile);

                // Check model availability and choose pipeline
                if (!_asrService.AreModelsAvailable)
                {
                    // No ASR models — cannot transcribe at all
                    Dispatcher.Invoke(() =>
                    {
                        TranscriptionStatus = "ASR models not downloaded. Go to Settings → Speech Recognition to download.";
                        TranscriptionStatusColor = Brushes.Orange;
                    });
                    return;
                }
                else if (_diarizationService.AreModelsAvailable)
                {
                    // Full pipeline: diarization + per-segment ASR
                    await RunDiarizationPipelineAsync(convertedFile);
                }
                else
                {
                    // ASR available but no diarization — single-pass transcription without speaker labels
                    Dispatcher.Invoke(() =>
                    {
                        DiarizationStatus = "Speaker diarization unavailable — download models in Settings";
                    });
                    await RunSinglePassTranscriptionAsync(convertedFile);
                }

                // Clean up the raw temp file (converted file cleaned up after transcription completes)
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    try { File.Delete(tempFile); } catch { }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = $"Audio processing error: {ex.Message}";
                    TranscriptionStatusColor = Brushes.Red;
                    IsDiarizing = false;
                });
            }
        }

        /// <summary>
        /// Single-pass transcription without speaker labels. Used when diarization models are not downloaded
        /// but ASR models are available.
        /// </summary>
        private async Task RunSinglePassTranscriptionAsync(string convertedWavPath)
        {
            Dispatcher.Invoke(() =>
            {
                TranscriptionStatus = "Transcribing audio...";
                TranscriptionStatusColor = Brushes.Blue;
            });

            try
            {
                var samples = AudioHelper.LoadWavAsFloats(convertedWavPath);
                var text = await _asrService.TranscribeAsync(samples);

                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        LiveTranscription = text;
                        TranscriptionStatus = "Transcription completed";
                        TranscriptionStatusColor = Brushes.Green;
                    }
                    else
                    {
                        TranscriptionStatus = "No speech detected in audio";
                        TranscriptionStatusColor = Brushes.Orange;
                    }
                });
            }
            finally
            {
                _ = Task.Delay(5000).ContinueWith(_ =>
                {
                    try { File.Delete(convertedWavPath); } catch { }
                });
            }
        }

        /// <summary>
        /// Full diarization pipeline: identify speakers → slice audio → transcribe per segment.
        /// </summary>
        private async Task RunDiarizationPipelineAsync(string convertedWavPath)
        {
            _diarizationCts = new CancellationTokenSource();
            var token = _diarizationCts.Token;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    IsDiarizing = true;
                    DiarizationStatus = "Running speaker diarization...";
                    DiarizationProgress = 0;
                });

                // Step 1: Run diarization to get speaker segments
                var settings = AppSettings.Diarization;
                var progress = new Progress<(int processed, int total)>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DiarizationProgress = p.total > 0 ? (double)p.processed / p.total * 100 : 0;
                        DiarizationStatus = $"Analyzing speakers... {DiarizationProgress:F0}%";
                    });
                });

                var segments = await _diarizationService.DiarizeAsync(
                    convertedWavPath,
                    settings.NumSpeakers,
                    settings.Threshold,
                    progress,
                    token);

                if (segments.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TranscriptionStatus = "No speech detected in audio";
                        TranscriptionStatusColor = Brushes.Orange;
                        IsDiarizing = false;
                        DiarizationStatus = "No speakers detected";
                    });
                    return;
                }

                // Load full audio once for all subsequent operations (post-merge, transcription, fingerprinting)
                var fullSamples = AudioHelper.LoadWavAsFloats(convertedWavPath);
                _lastFullAudioSamples = fullSamples;

                // Post-processing: merge over-segmented speakers by embedding similarity
                if (settings.EnablePostMerge && segments.Count > 0)
                {
                    var uniqueBefore = segments.Select(s => s.Speaker).Distinct().Count();

                    if (uniqueBefore > 1)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DiarizationStatus = $"Analyzing {uniqueBefore} speaker embeddings for merging...";
                        });

                        segments = await SherpaOnnxDiarizationService.MergeSimilarSpeakersAsync(
                            segments, fullSamples, settings.PostMergeThreshold);

                        var uniqueAfter = segments.Select(s => s.Speaker).Distinct().Count();

                        if (uniqueAfter < uniqueBefore)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DiarizationStatus = $"Merged {uniqueBefore} → {uniqueAfter} speakers";
                            });
                        }
                    }
                }

                _lastDiarizationSegments = segments;

                var speakerCount = segments.Select(s => s.Speaker).Distinct().Count();
                Dispatcher.Invoke(() =>
                {
                    DetectedSpeakerCount = speakerCount;
                    DiarizationStatus = $"Found {speakerCount} speaker{(speakerCount != 1 ? "s" : "")}. Transcribing segments...";
                    DiarizationProgress = 100;
                });
                var transcriptionSegments = new List<TranscriptionSegment>();

                for (int i = 0; i < segments.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var seg = segments[i];
                    var segDuration = seg.End - seg.Start;

                    // Skip very short segments (< 0.3 seconds) — unlikely to contain recognizable speech
                    if (segDuration < 0.3f)
                        continue;

                    Dispatcher.Invoke(() =>
                    {
                        DiarizationStatus = $"Transcribing segment {i + 1}/{segments.Count} (Speaker {seg.Speaker + 1})...";
                    });

                    // Extract float[] sub-array for this segment's time range (16kHz)
                    int startIndex = (int)(seg.Start * 16000);
                    int endIndex = Math.Min((int)(seg.End * 16000), fullSamples.Length);
                    int length = endIndex - startIndex;

                    if (length <= 0)
                        continue;

                    var segmentSamples = new float[length];
                    Array.Copy(fullSamples, startIndex, segmentSamples, 0, length);

                    // Transcribe using sherpa-onnx ASR
                    var text = await _asrService.TranscribeAsync(segmentSamples);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        transcriptionSegments.Add(new TranscriptionSegment
                        {
                            SpeakerIndex = seg.Speaker,
                            SpeakerLabel = $"Speaker {seg.Speaker + 1}",
                            Text = text,
                            StartSeconds = seg.Start,
                            EndSeconds = seg.End
                        });
                    }
                }

                // Step 3: Build DiarizedTranscription and update UI
                _diarizedTranscription = new DiarizedTranscription
                {
                    Segments = transcriptionSegments,
                    SpeakerCount = speakerCount,
                    TotalDuration = TimeSpan.FromSeconds(segments.Last().End)
                };

                Dispatcher.Invoke(() =>
                {
                    LiveTranscription = _diarizedTranscription.ToFlatText();
                    TranscriptionStatus = "Transcription completed";
                    TranscriptionStatusColor = Brushes.Green;
                    IsDiarizing = false;
                    DiarizationStatus = $"Complete: {speakerCount} speaker{(speakerCount != 1 ? "s" : "")}, {transcriptionSegments.Count} segments";

                    // Show speaker identification panel
                    PopulateSpeakerPanel();
                });

                // Run speaker identification pipeline (non-blocking):
                // 1. Voice fingerprint matching against stored profiles
                // 2. LLM-based name inference for remaining unknown speakers
                _ = RunSpeakerIdentificationPipelineAsync(fullSamples, segments, token);
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = "Diarization cancelled";
                    TranscriptionStatusColor = Brushes.Orange;
                    IsDiarizing = false;
                    DiarizationStatus = "Cancelled";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = $"Diarization error: {ex.Message}";
                    TranscriptionStatusColor = Brushes.Red;
                    IsDiarizing = false;
                    DiarizationStatus = $"Error: {ex.Message}";
                });
            }
            finally
            {
                // Clean up converted WAV file
                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    try { File.Delete(convertedWavPath); } catch { }
                });

                _diarizationCts?.Dispose();
                _diarizationCts = null;
            }
        }

        private string ConvertAudioForSpeechRecognition(string inputFile)
        {
            try
            {
                // Create a converted file with standard format for speech recognition
                var convertedFile = Path.GetTempFileName() + "_converted.wav";
                
                using (var reader = new AudioFileReader(inputFile))
                {
                    // Convert to standard format: 16kHz, 16-bit, mono
                    var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 1))
                    {
                        ResamplerQuality = 60
                    };
                    
                    WaveFileWriter.CreateWaveFile(convertedFile, resampler);
                    resampler.Dispose();
                }
                
                return convertedFile;
            }
            catch
            {
                // If conversion fails, return original file
                return inputFile;
            }
        }


        private void OnToggleRecordingClicked(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            try
            {
                if (_loopbackCapture != null)
                {
                    // Clear previous audio data
                    _audioStream?.SetLength(0);
                    
                    // Start capturing system audio (speakers/headphones output)
                    _loopbackCapture.StartRecording();
                    
                    _isRecording = true;
                    RecordingButtonText = "Stop Recording";
                    RecordingButtonColor = Brushes.Red;
                    TranscriptionStatus = "Recording... Click Stop when done";
                    TranscriptionStatusColor = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                TranscriptionStatus = $"Recording error: {ex.Message}";
                TranscriptionStatusColor = Brushes.Red;
            }
        }

        private void StopRecording()
        {
            try
            {
                if (_loopbackCapture != null && _isRecording)
                {
                    _isRecording = false;
                    RecordingButtonText = "Start Recording";
                    RecordingButtonColor = Brushes.Green;
                    TranscriptionStatus = "Processing audio...";
                    TranscriptionStatusColor = Brushes.Orange;

                    // Stop capturing — this triggers OnSystemAudioRecordingStopped which calls ProcessCapturedAudio
                    _loopbackCapture.StopRecording();
                }
            }
            catch (Exception ex)
            {
                TranscriptionStatus = $"Stop error: {ex.Message}";
                TranscriptionStatusColor = Brushes.Red;
            }
        }
        


        public void SetTestData()
        {
            MeetingTitle = "Test Meeting - Column Verification";
            LiveTranscription = "This is a test transcription to verify all Notion columns are working correctly. We discussed various topics including project timelines, resource allocation, and upcoming deliverables.";
            ManualNotes = "Test notes: Need to follow up on budget approval and schedule team meeting for next week.";
            AiSummary = "### Meeting Overview\nTest meeting to verify Notion integration and column mappings.\n\n### Action Items\n- [ ] Test user to verify all columns by end of day\n- [ ] Team to review test results";
            
            // Add test key points
            KeyPoints.Clear();
            KeyPoints.Add(new KeyPoint { Text = "All columns are mapped correctly", IsCompleted = false });
            KeyPoints.Add(new KeyPoint { Text = "Rich text formatting works", IsCompleted = false });
            KeyPoints.Add(new KeyPoint { Text = "Date fields are properly formatted", IsCompleted = false });
            
            // Add test action items
            ActionItems.Clear();
            ActionItems.Add(new ActionItem { Text = "Verify Title column", Assignee = "Test User", IsCompleted = false });
            ActionItems.Add(new ActionItem { Text = "Check Rich Text columns", Assignee = "Team", IsCompleted = false });
            ActionItems.Add(new ActionItem { Text = "Validate Date field", Assignee = "Test User", IsCompleted = false });
            
            MeetingStartTime = DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss");
            MeetingDuration = "01:00:00";
        }
    }

    public class KeyPoint
    {
        public string Text { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ActionItem
    {
        public string Text { get; set; }
        public string Assignee { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class SpeakerEntry : INotifyPropertyChanged
    {
        private string _name = "";
        private string _confidenceBadge = "";
        private Brush _confidenceColor = Brushes.Transparent;

        public int SpeakerIndex { get; set; }
        public string DefaultLabel { get; set; } = "";

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string ConfidenceBadge
        {
            get => _confidenceBadge;
            set
            {
                _confidenceBadge = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfidenceBadge)));
            }
        }

        public Brush ConfidenceColor
        {
            get => _confidenceColor;
            set
            {
                _confidenceColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfidenceColor)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}