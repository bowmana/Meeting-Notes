using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MeetingNotesApp
{
    /// <summary>
    /// Interface for speaker diarization — identifies "who spoke when" from audio.
    /// Returns time-stamped speaker segments that can be paired with transcription.
    /// </summary>
    public interface ISpeakerDiarizationService : IDisposable
    {
        /// <summary>Whether the required diarization models are downloaded and available.</summary>
        bool AreModelsAvailable { get; }

        /// <summary>
        /// Run offline speaker diarization on a WAV file.
        /// Returns speaker segments sorted by start time.
        /// </summary>
        /// <param name="wavFilePath">Path to a 16kHz mono WAV file.</param>
        /// <param name="numSpeakers">Number of speakers (-1 for auto-detect).</param>
        /// <param name="threshold">Clustering threshold (used when numSpeakers is -1). Lower = more speakers.</param>
        /// <param name="progress">Reports (processedChunks, totalChunks) during processing.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of segments with start time, end time, and speaker index (0-based).</returns>
        Task<List<(float Start, float End, int Speaker)>> DiarizeAsync(
            string wavFilePath,
            int numSpeakers = -1,
            float threshold = 0.5f,
            IProgress<(int processed, int total)>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
