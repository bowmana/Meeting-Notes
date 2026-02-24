using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NAudio.CoreAudioApi;

namespace MeetingNotesApp
{
    public sealed class CallDetectionService : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        public event EventHandler<MeetingDetectedEventArgs>? MeetingDetected;
        public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
        public event EventHandler<DetectionErrorEventArgs>? DetectionError;

        private const int PollIntervalMs = 2000;
        private const int InactiveThreshold = 3;

        private readonly DispatcherTimer _pollTimer;
        private readonly List<PlatformDetector> _detectors;
        private bool _isMeetingActive;
        private int _consecutiveInactiveCount;
        private string? _activePlatformName;
        private bool _isEnabled;
        private bool _disposed;

        public CallDetectionService()
        {
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PollIntervalMs)
            };
            _pollTimer.Tick += OnPollTick;

            _detectors = new List<PlatformDetector>
            {
                new PlatformDetector("Zoom", new[] { "Zoom" }, "Zoom Meeting")
            };
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public bool IsMeetingActive => _isMeetingActive;

        public string? ActivePlatformName => _activePlatformName;

        public void Start()
        {
            if (!_pollTimer.IsEnabled)
                _pollTimer.Start();
        }

        public void Stop()
        {
            _pollTimer.Stop();
        }

        public void ScanNow()
        {
            OnPollTick(this, EventArgs.Empty);
        }

        private void OnPollTick(object? sender, EventArgs e)
        {
            if (!_isEnabled)
                return;

            try
            {
                DetectionResult? result = null;

                foreach (var detector in _detectors)
                {
                    bool hasWindow = CheckMeetingWindow(detector.MeetingWindowTitle);
                    bool hasActiveAudio = CheckAudioSessionWithAudio(detector.ProcessNames);

                    // Meeting window is the strongest signal (Zoom only shows "Zoom Meeting" window during calls).
                    // Active audio session (with sound flowing) is a secondary confirmation.
                    // Just having an audio session registered is NOT enough — Zoom keeps sessions alive when idle.
                    if (hasWindow || hasActiveAudio)
                    {
                        string method;
                        if (hasWindow && hasActiveAudio)
                            method = "Window Title + Active Audio";
                        else if (hasWindow)
                            method = "Window Title";
                        else
                            method = "Active Audio";

                        result = new DetectionResult(detector.PlatformName, method);
                        break;
                    }
                }

                if (result != null)
                {
                    _consecutiveInactiveCount = 0;

                    if (!_isMeetingActive)
                    {
                        _isMeetingActive = true;
                        _activePlatformName = result.PlatformName;
                        MeetingDetected?.Invoke(this, new MeetingDetectedEventArgs(
                            result.PlatformName, result.DetectionMethod));
                    }
                }
                else
                {
                    if (_isMeetingActive)
                    {
                        _consecutiveInactiveCount++;

                        if (_consecutiveInactiveCount >= InactiveThreshold)
                        {
                            var endedPlatform = _activePlatformName ?? "Unknown";
                            _isMeetingActive = false;
                            _activePlatformName = null;
                            _consecutiveInactiveCount = 0;
                            MeetingEnded?.Invoke(this, new MeetingEndedEventArgs(endedPlatform));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DetectionError?.Invoke(this, new DetectionErrorEventArgs(
                    $"Detection scan failed: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Checks if any audio session belonging to the target process has active audio flowing.
        /// Just having a registered session is NOT enough — Zoom keeps idle sessions alive.
        /// We check MasterPeakValue > threshold to confirm actual audio output.
        /// </summary>
        private bool CheckAudioSessionWithAudio(string[] processNames)
        {
            const float audioThreshold = 0.001f;

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    try
                    {
                        var sessionManager = device.AudioSessionManager;
                        var sessions = sessionManager.Sessions;

                        for (int i = 0; i < sessions.Count; i++)
                        {
                            var session = sessions[i];
                            try
                            {
                                var processId = (int)session.GetProcessID;
                                if (processId == 0) continue;

                                var process = Process.GetProcessById(processId);
                                var procName = process.ProcessName;

                                if (processNames.Any(pn =>
                                    string.Equals(procName, pn, StringComparison.OrdinalIgnoreCase)))
                                {
                                    float peak = session.AudioMeterInformation.MasterPeakValue;
                                    if (peak > audioThreshold)
                                        return true;
                                }
                            }
                            catch (ArgumentException) { }
                            catch (InvalidOperationException) { }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }

            return false;
        }

        private bool CheckMeetingWindow(string? meetingWindowTitle)
        {
            if (string.IsNullOrEmpty(meetingWindowTitle))
                return false;

            try
            {
                var hwnd = FindWindow(null, meetingWindowTitle);
                return hwnd != IntPtr.Zero;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pollTimer.Stop();
        }

        private record DetectionResult(string PlatformName, string DetectionMethod);
    }

    public sealed class PlatformDetector
    {
        public string PlatformName { get; }
        public string[] ProcessNames { get; }
        public string? MeetingWindowTitle { get; }

        public PlatformDetector(string platformName, string[] processNames, string? meetingWindowTitle)
        {
            PlatformName = platformName;
            ProcessNames = processNames;
            MeetingWindowTitle = meetingWindowTitle;
        }
    }

    public sealed class MeetingDetectedEventArgs : EventArgs
    {
        public string PlatformName { get; }
        public string DetectionMethod { get; }

        public MeetingDetectedEventArgs(string platformName, string detectionMethod)
        {
            PlatformName = platformName;
            DetectionMethod = detectionMethod;
        }
    }

    public sealed class MeetingEndedEventArgs : EventArgs
    {
        public string PlatformName { get; }

        public MeetingEndedEventArgs(string platformName)
        {
            PlatformName = platformName;
        }
    }

    public sealed class DetectionErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        public DetectionErrorEventArgs(string errorMessage, Exception? exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
