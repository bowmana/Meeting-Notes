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

        // Speaker diarization fields
        private ISpeakerDiarizationService _diarizationService;
        private DiarizedTranscription _diarizedTranscription;
        private CancellationTokenSource _diarizationCts;
        private string _diarizationStatus = "";
        private double _diarizationProgress = 0;
        private bool _isDiarizing = false;
        private int _detectedSpeakerCount = 0;

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

            // Initialize meeting info if provided
            if (_meetingInfo != null)
            {
                MeetingTitle = _meetingInfo.Title;
                MeetingStartTime = _meetingInfo.StartTime.ToString("MM/dd/yy HH:mm");
                MeetingPlatform = _meetingInfo.Workspace?.WorkspaceName ?? "Unknown Platform";
                
                // Set database info
                var workspace = _meetingInfo.Workspace;
                if (workspace?.SelectedDatabase != null)
                {
                    var dbName = workspace.SelectedDatabase.Name;
                    // Remove the workspace suffix if it's already there
                    if (dbName.Contains($"({workspace.WorkspaceName})"))
                    {
                        DatabaseInfo = dbName;
                    }
                    else
                    {
                        DatabaseInfo = $"{dbName} ({workspace.WorkspaceName})";
                    }
                }
                else
                {
                    DatabaseInfo = "No Database Selected";
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

        private async void OnSaveToNotionClicked(object sender, RoutedEventArgs e)
        {
            if (_meetingInfo?.Workspace == null || _meetingInfo.Workspace.SelectedDatabase == null)
            {
                return;
            }

            try
            {
                // Disable the button to prevent multiple clicks
                var button = sender as Button;
                button.IsEnabled = false;
                button.Content = "Saving...";

                await SaveToNotionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to Notion: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable the button
                var button = sender as Button;
                button.IsEnabled = true;
                button.Content = "Save to Notion";
            }
        }

        private async Task SaveToNotionAsync()
        {
            var workspace = _meetingInfo.Workspace;
            var database = workspace.SelectedDatabase;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workspace.ApiKey);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            // Format key points as bullet list
            var keyPointsText = string.Join("\n", KeyPoints.Select(kp => $"• {kp.Text}"));

            // Format action items as bullet list with assignees
            var actionItemsText = string.Join("\n", ActionItems.Select(ai => $"• {ai.Text} (Assigned to: {ai.Assignee})"));

            // Parse start time
            DateTime? startTime = null;
            if (DateTime.TryParse(MeetingStartTime, out var parsedTime))
            {
                startTime = parsedTime;
            }

            var properties = new Dictionary<string, object>
            {
                ["Title"] = new
                {
                    title = new[]
                    {
                        new { text = new { content = MeetingTitle } }
                    }
                },
                ["Transcription"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = LiveTranscription } }
                    }
                },
                ["My Notes"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = ManualNotes } }
                    }
                },
                ["AI Summary"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = AiSummary } }
                    }
                },
                ["Key Points"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = keyPointsText } }
                    }
                },
                ["Action Items"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = actionItemsText } }
                    }
                },
                ["Duration"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = MeetingDuration } }
                    }
                },
                ["Organizer"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = MeetingOrganizer } }
                    }
                },
                ["Attendees"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = MeetingAttendees } }
                    }
                }
            };

            // Add Speakers property if diarization was used
            if (_diarizedTranscription != null && _diarizedTranscription.SpeakerCount > 0)
            {
                properties["Speakers"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = $"{_diarizedTranscription.SpeakerCount} speakers detected" } }
                    }
                };
            }

            // Truncate transcription to Notion's 2000-char rich_text limit
            if (properties.ContainsKey("Transcription"))
            {
                var transcriptionText = LiveTranscription ?? "";
                if (transcriptionText.Length > 2000)
                {
                    transcriptionText = transcriptionText.Substring(0, 1990) + "\n... (truncated)";
                }
                properties["Transcription"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = transcriptionText } }
                    }
                };
            }

            // Add Start Time if available
            if (startTime.HasValue)
            {
                properties["Date"] = new
                {
                    date = new { start = startTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };
            }

            var requestBody = new
            {
                parent = new { database_id = database.Id },
                properties = properties
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            // Debug info removed per user request

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.notion.com/v1/pages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Notion API error: {response.StatusCode} - {errorContent}";
                MessageBox.Show(errorMessage, "Notion API Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new Exception(errorMessage);
            }
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

                var speakerCount = segments.Max(s => s.Speaker) + 1;
                Dispatcher.Invoke(() =>
                {
                    DetectedSpeakerCount = speakerCount;
                    DiarizationStatus = $"Found {speakerCount} speaker{(speakerCount != 1 ? "s" : "")}. Transcribing segments...";
                    DiarizationProgress = 100;
                });

                // Step 2: Load full audio once as float[], then transcribe each segment via sub-array
                var fullSamples = AudioHelper.LoadWavAsFloats(convertedWavPath);
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
                });
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
}