using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeetingNotesApp
{
    public class SpeakerProfile
    {
        public string Name { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public int MeetingCount { get; set; } = 1;
    }

    /// <summary>
    /// Manages persistent speaker voice profiles for cross-meeting speaker identification.
    /// Profiles are stored locally at %AppData%/MeetingNotesApp/speaker_profiles.json.
    /// Uses sherpa-onnx SpeakerEmbeddingExtractor (same model as diarization).
    /// </summary>
    public class SpeakerProfileService : IDisposable
    {
        private static readonly string ProfilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingNotesApp");

        private static readonly string ProfilesPath = Path.Combine(ProfilesDir, "speaker_profiles.json");

        private List<SpeakerProfile> _profiles = new();
        private readonly SpeakerEmbeddingHelper _embeddingHelper;
        private bool _disposed;

        public SpeakerProfileService(SpeakerEmbeddingHelper embeddingHelper)
        {
            _embeddingHelper = embeddingHelper;
            LoadProfiles();
        }

        public bool IsAvailable => _embeddingHelper.IsModelAvailable;

        public IReadOnlyList<SpeakerProfile> Profiles => _profiles.AsReadOnly();

        /// <summary>
        /// Identifies a speaker by comparing their audio embedding against stored profiles.
        /// Returns the matching profile name and confidence, or null if no match above threshold.
        /// </summary>
        public async Task<(string? Name, float Confidence)> IdentifySpeakerAsync(
            float[] samples, int sampleRate = 16000, float threshold = 0.6f)
        {
            if (_profiles.Count == 0 || !IsAvailable)
                return (null, 0f);

            var embedding = await _embeddingHelper.ExtractEmbeddingAsync(samples, sampleRate);
            if (embedding.Length == 0)
                return (null, 0f);

            return FindBestMatch(embedding, threshold);
        }

        /// <summary>
        /// Identifies a speaker from pre-computed embedding.
        /// </summary>
        public (string? Name, float Confidence) IdentifySpeaker(float[] embedding, float threshold = 0.6f)
        {
            if (_profiles.Count == 0 || embedding.Length == 0)
                return (null, 0f);

            return FindBestMatch(embedding, threshold);
        }

        /// <summary>
        /// Enrolls a new speaker or updates an existing speaker's profile.
        /// </summary>
        public async Task EnrollSpeakerAsync(string name, float[] samples, int sampleRate = 16000)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Speaker name cannot be empty.", nameof(name));

            var embedding = await _embeddingHelper.ExtractEmbeddingAsync(samples, sampleRate);
            if (embedding.Length == 0)
                throw new InvalidOperationException("Could not extract speaker embedding from audio.");

            EnrollWithEmbedding(name, embedding);
        }

        /// <summary>
        /// Enrolls a speaker using a pre-computed embedding vector.
        /// </summary>
        public void EnrollWithEmbedding(string name, float[] embedding)
        {
            var existing = _profiles.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Update: running average of embeddings for better accuracy
                existing.Embedding = AverageEmbeddings(existing.Embedding, embedding);
                existing.LastSeen = DateTime.UtcNow;
                existing.MeetingCount++;
            }
            else
            {
                _profiles.Add(new SpeakerProfile
                {
                    Name = name,
                    Embedding = embedding,
                    LastSeen = DateTime.UtcNow,
                    MeetingCount = 1
                });
            }

            SaveProfiles();
        }

        public void DeleteProfile(string name)
        {
            _profiles.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            SaveProfiles();
        }

        public void ClearAllProfiles()
        {
            _profiles.Clear();
            SaveProfiles();
        }

        private (string? Name, float Confidence) FindBestMatch(float[] embedding, float threshold)
        {
            string? bestName = null;
            float bestSimilarity = 0f;

            foreach (var profile in _profiles)
            {
                if (profile.Embedding.Length != embedding.Length)
                    continue;

                var similarity = CosineSimilarity(embedding, profile.Embedding);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestName = profile.Name;
                }
            }

            if (bestSimilarity >= threshold)
                return (bestName, bestSimilarity);

            return (null, bestSimilarity);
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0f;

            float dot = 0f, normA = 0f, normB = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0f || normB == 0f) return 0f;
            return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
        }

        private static float[] AverageEmbeddings(float[] existing, float[] newEmb)
        {
            if (existing.Length != newEmb.Length)
                return newEmb;

            var result = new float[existing.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = (existing[i] + newEmb[i]) / 2f;

            return result;
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(ProfilesPath))
                {
                    var json = File.ReadAllText(ProfilesPath);
                    _profiles = JsonSerializer.Deserialize<List<SpeakerProfile>>(json) ?? new();
                }
            }
            catch
            {
                _profiles = new();
            }
        }

        private void SaveProfiles()
        {
            try
            {
                Directory.CreateDirectory(ProfilesDir);
                var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ProfilesPath, json);
            }
            catch
            {
                // Best effort — don't crash if we can't save profiles
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Don't dispose _embeddingHelper — it's owned by the caller
                _disposed = true;
            }
        }
    }
}
