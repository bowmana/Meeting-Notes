using System;
using System.Threading.Tasks;
using SherpaOnnx;

namespace MeetingNotesApp
{
    /// <summary>
    /// Wraps sherpa-onnx SpeakerEmbeddingExtractor for extracting speaker voice embeddings.
    /// Reuses the same 3D-Speaker CampPlus model already downloaded for diarization.
    /// </summary>
    public class SpeakerEmbeddingHelper : IDisposable
    {
        private SpeakerEmbeddingExtractor? _extractor;
        private bool _disposed;

        public bool IsModelAvailable => DiarizationModelManager.IsEmbeddingModelDownloaded;

        public int EmbeddingDimension
        {
            get
            {
                EnsureExtractor();
                return _extractor!.Dim;
            }
        }

        /// <summary>
        /// Extracts a speaker embedding vector from audio samples (16kHz, mono, float32).
        /// </summary>
        public async Task<float[]> ExtractEmbeddingAsync(float[] samples, int sampleRate = 16000)
        {
            if (!IsModelAvailable)
                throw new InvalidOperationException(
                    "Speaker embedding model not available. Download diarization models first.");

            return await Task.Run(() =>
            {
                EnsureExtractor();

                var stream = _extractor!.CreateStream();
                try
                {
                    stream.AcceptWaveform(sampleRate, samples);
                    stream.InputFinished();

                    if (!_extractor.IsReady(stream))
                        return Array.Empty<float>();

                    return _extractor.Compute(stream);
                }
                finally
                {
                    stream.Dispose();
                }
            });
        }

        /// <summary>
        /// Extracts an embedding from a subset of a larger audio buffer.
        /// Combines multiple segments for the same speaker into one embedding.
        /// </summary>
        public async Task<float[]> ExtractEmbeddingFromSegmentsAsync(
            float[] fullAudio, int sampleRate,
            (float Start, float End)[] segments,
            float maxSeconds = 20f)
        {
            // Combine up to maxSeconds of audio from the speaker's segments
            int maxSamples = (int)(maxSeconds * sampleRate);
            var combined = new float[Math.Min(maxSamples, fullAudio.Length)];
            int written = 0;

            foreach (var seg in segments)
            {
                if (written >= maxSamples) break;

                int start = (int)(seg.Start * sampleRate);
                int end = Math.Min((int)(seg.End * sampleRate), fullAudio.Length);
                int length = Math.Min(end - start, maxSamples - written);

                if (length <= 0) continue;

                Array.Copy(fullAudio, start, combined, written, length);
                written += length;
            }

            if (written == 0)
                return Array.Empty<float>();

            // Trim to actual length if we didn't fill the buffer
            if (written < combined.Length)
            {
                var trimmed = new float[written];
                Array.Copy(combined, trimmed, written);
                combined = trimmed;
            }

            return await ExtractEmbeddingAsync(combined, sampleRate);
        }

        private void EnsureExtractor()
        {
            if (_extractor != null) return;

            var config = new SpeakerEmbeddingExtractorConfig();
            config.Model = DiarizationModelManager.EmbeddingModelPath;
            config.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            config.Debug = 0;

            _extractor = new SpeakerEmbeddingExtractor(config);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _extractor?.Dispose();
                _extractor = null;
                _disposed = true;
            }
        }
    }
}
