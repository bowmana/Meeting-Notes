using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using SherpaOnnx;

namespace MeetingNotesApp
{
    /// <summary>
    /// Speaker diarization service using sherpa-onnx's offline diarization pipeline.
    /// Processes complete audio files (post-recording, not real-time).
    /// Pipeline: pyannote segmentation → 3D-Speaker embedding → spectral clustering.
    /// </summary>
    public class SherpaOnnxDiarizationService : ISpeakerDiarizationService
    {
        private OfflineSpeakerDiarization? _diarizer;
        private bool _disposed;

        public bool AreModelsAvailable => DiarizationModelManager.AreModelsDownloaded;

        /// <summary>
        /// Run offline speaker diarization on a 16kHz mono WAV file.
        /// </summary>
        public async Task<List<(float Start, float End, int Speaker)>> DiarizeAsync(
            string wavFilePath,
            int numSpeakers = -1,
            float threshold = 0.5f,
            IProgress<(int processed, int total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!AreModelsAvailable)
                throw new InvalidOperationException(
                    "Speaker diarization models are not downloaded. Go to Settings → Speaker Diarization to download them.");

            if (!File.Exists(wavFilePath))
                throw new FileNotFoundException($"Audio file not found: {wavFilePath}");

            // Load audio as float[] normalized to [-1, 1]
            var samples = AudioHelper.LoadWavAsFloats(wavFilePath);

            if (samples.Length == 0)
                throw new InvalidOperationException("Audio file contains no samples.");

            // Run diarization on a background thread to keep UI responsive
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create or reconfigure the diarization engine
                var config = CreateConfig(numSpeakers, threshold);
                var diarizer = new OfflineSpeakerDiarization(config);

                try
                {
                    // Verify sample rate matches (should be 16000)
                    if (diarizer.SampleRate != 16000)
                        throw new InvalidOperationException(
                            $"sherpa-onnx expects {diarizer.SampleRate}Hz audio, but pipeline provides 16000Hz.");

                    // Process with progress callback
                    OfflineSpeakerDiarizationSegment[] rawSegments;

                    if (progress != null)
                    {
                        var callback = new OfflineSpeakerDiarizationProgressCallback(
                            (int numProcessedChunks, int numTotalChunks, IntPtr arg) =>
                            {
                                progress.Report((numProcessedChunks, numTotalChunks));

                                // Return non-zero to abort if cancellation requested
                                return cancellationToken.IsCancellationRequested ? 1 : 0;
                            });

                        rawSegments = diarizer.ProcessWithCallback(samples, callback, IntPtr.Zero);
                    }
                    else
                    {
                        rawSegments = diarizer.Process(samples);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (rawSegments == null || rawSegments.Length == 0)
                        return new List<(float, float, int)>();

                    // Convert to tuple list and merge adjacent same-speaker segments
                    var segments = rawSegments
                        .Select(s => (Start: s.Start, End: s.End, Speaker: s.Speaker))
                        .OrderBy(s => s.Start)
                        .ToList();

                    return MergeAdjacentSegments(segments);
                }
                finally
                {
                    diarizer.Dispose();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Creates the sherpa-onnx diarization configuration with model paths and clustering settings.
        /// </summary>
        private static OfflineSpeakerDiarizationConfig CreateConfig(int numSpeakers, float threshold)
        {
            // IMPORTANT: All sherpa-onnx config types are STRUCTS with [StructLayout(LayoutKind.Sequential)].
            // You CANNOT chain property access (e.g. config.Segmentation.Pyannote.Model = ...) because
            // each property access returns a COPY. Must copy to local, modify, then assign back.
            var config = new OfflineSpeakerDiarizationConfig();

            // Segmentation model (user-selected: Pyannote 3.0 or Reverb v1)
            var segmentation = config.Segmentation;
            var pyannote = segmentation.Pyannote;
            pyannote.Model = DiarizationModelManager.GetSegmentationModelPath(
                AppSettings.Diarization.SelectedSegmentationModel);
            segmentation.Pyannote = pyannote;
            segmentation.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            segmentation.Provider = "cpu";
            config.Segmentation = segmentation;

            // Speaker embedding model (3D-Speaker CampPlus)
            var embedding = config.Embedding;
            embedding.Model = DiarizationModelManager.EmbeddingModelPath;
            embedding.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            embedding.Provider = "cpu";
            config.Embedding = embedding;

            // Clustering
            var clustering = config.Clustering;
            clustering.NumClusters = numSpeakers; // -1 = auto-detect
            clustering.Threshold = threshold;
            config.Clustering = clustering;

            // Minimum durations
            config.MinDurationOn = AppSettings.Diarization.MinDurationOn;
            config.MinDurationOff = AppSettings.Diarization.MinDurationOff;

            return config;
        }

        /// <summary>
        /// Merges consecutive segments from the same speaker when the gap between them
        /// is smaller than the minimum silence duration. This reduces noise and produces
        /// cleaner, longer segments for transcription.
        /// </summary>
        internal static List<(float Start, float End, int Speaker)> MergeAdjacentSegments(
            List<(float Start, float End, int Speaker)> segments, float gapThreshold = 0.5f)
        {
            if (segments.Count <= 1)
                return segments;

            var merged = new List<(float Start, float End, int Speaker)>();
            var current = segments[0];

            for (int i = 1; i < segments.Count; i++)
            {
                var next = segments[i];

                // Merge if same speaker and gap is small enough
                if (next.Speaker == current.Speaker && (next.Start - current.End) <= gapThreshold)
                {
                    current = (current.Start, next.End, current.Speaker);
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }

            merged.Add(current);
            return merged;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _diarizer?.Dispose();
                _diarizer = null;
                _disposed = true;
            }
        }
    }
}
