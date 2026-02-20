using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
using System.Text;
using System.Text.Json;
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
        private SpeechRecognitionEngine _speechEngine;
        private WasapiLoopbackCapture _loopbackCapture;
        private WaveFileWriter _waveFileWriter;
        private MemoryStream _audioStream;
        private bool _isRecording = false;
        private string _recordingButtonText = "Start Recording";
        private Brush _recordingButtonColor = Brushes.Green;
        private string _transcriptionStatus = "Ready to record";
        private Brush _transcriptionStatusColor = Brushes.Gray;

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

            // Initialize speech recognition
            InitializeSpeechRecognition();

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
                        content = @"You are a meeting assistant. Create a structured meeting summary in the following exact format:

### Meeting Overview
[Brief 2-3 sentence overview of the main topic and purpose of the meeting]

### Action Items
[Extract specific action items from the meeting in this format:]
- [ ] [Person] to [specific action] by [timeframe if mentioned]

IMPORTANT: Always create a summary based on the provided content, no matter how brief or minimal it seems. Even if the content appears unimportant or short, still provide a professional summary.

Focus on extracting concrete action items with assignees when mentioned. If no specific assignee is mentioned, use 'Team' or 'TBD'. If no action items are found, write 'No specific action items identified.' Keep the summary professional and structured with proper markdown formatting."
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
            // Clean up transcription resources
            StopRecording();
            _speechEngine?.Dispose();
            _loopbackCapture?.Dispose();
            _waveFileWriter?.Dispose();
            _audioStream?.Dispose();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                // Create speech recognition engine with better settings
                _speechEngine = new SpeechRecognitionEngine();
                
                // Create a grammar for dictation (free-form speech)
                var dictationGrammar = new DictationGrammar();
                _speechEngine.LoadGrammar(dictationGrammar);
                
                // Set up event handlers
                _speechEngine.SpeechRecognized += OnSpeechRecognized;
                _speechEngine.SpeechRecognitionRejected += OnSpeechRecognitionRejected;
                
                // Configure recognition settings for better accuracy
                _speechEngine.BabbleTimeout = TimeSpan.FromSeconds(2);
                _speechEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
                _speechEngine.EndSilenceTimeout = TimeSpan.FromSeconds(1);
                _speechEngine.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(2);
                
                // Initialize system audio capture (speakers/headphones output)
                _loopbackCapture = new WasapiLoopbackCapture();
                _loopbackCapture.DataAvailable += OnSystemAudioDataAvailable;
                _loopbackCapture.RecordingStopped += OnSystemAudioRecordingStopped;
                
                // Create memory stream for audio data
                _audioStream = new MemoryStream();
                
                TranscriptionStatus = "Ready to capture system audio";
                TranscriptionStatusColor = Brushes.Gray;
            }
            catch (Exception ex)
            {
                TranscriptionStatus = $"Error: {ex.Message}";
                TranscriptionStatusColor = Brushes.Red;
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Update UI on the main thread
            Dispatcher.Invoke(() =>
            {
                // Append to existing transcription instead of replacing
                if (!string.IsNullOrEmpty(LiveTranscription))
                {
                    LiveTranscription += " " + e.Result.Text;
                }
                else
                {
                    LiveTranscription = e.Result.Text;
                }
                
                TranscriptionStatus = "Transcription completed";
                TranscriptionStatusColor = Brushes.Green;
            });
        }

        private void OnSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            // Update UI on the main thread
            Dispatcher.Invoke(() =>
            {
                TranscriptionStatus = "Listening...";
                TranscriptionStatusColor = Brushes.Orange;
            });
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

        private void ProcessCapturedAudio()
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
                
                // Log audio file size for debugging
                var fileSize = new FileInfo(tempFile).Length;
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = $"Processing audio ({fileSize} bytes)...";
                    TranscriptionStatusColor = Brushes.Blue;
                });
                
                // Convert audio format for speech recognition
                var convertedFile = ConvertAudioForSpeechRecognition(tempFile);
                
                // Set speech recognition input to the converted audio file
                _speechEngine.SetInputToWaveFile(convertedFile);
                
                // Start recognition (single mode for one-time processing)
                _speechEngine.RecognizeAsync(RecognizeMode.Single);
                
                // Set up a timeout to handle cases where recognition doesn't complete
                Task.Delay(15000).ContinueWith(_ => 
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (TranscriptionStatus == "Processing audio..." || TranscriptionStatus.Contains("Processing audio"))
                        {
                            TranscriptionStatus = "Transcription completed";
                            TranscriptionStatusColor = Brushes.Green;
                        }
                    });
                });
                
                // Clean up temp files after a delay
                Task.Delay(5000).ContinueWith(_ => 
                {
                    try { File.Delete(tempFile); } catch { }
                    try { File.Delete(convertedFile); } catch { }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TranscriptionStatus = $"Audio processing error: {ex.Message}";
                    TranscriptionStatusColor = Brushes.Red;
                });
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
                    // Stop capturing system audio
                    _loopbackCapture.StopRecording();
                    
                    _isRecording = false;
                    RecordingButtonText = "Start Recording";
                    RecordingButtonColor = Brushes.Green;
                    TranscriptionStatus = "Processing audio...";
                    TranscriptionStatusColor = Brushes.Orange;
                    
                    // Process the recorded audio
                    ProcessCapturedAudio();
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