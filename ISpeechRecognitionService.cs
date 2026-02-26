using System;
using System.Threading.Tasks;

namespace MeetingNotesApp
{
    /// <summary>
    /// Interface for speech recognition — converts audio to text.
    /// Operates on float[] audio samples (16kHz, mono, normalized [-1,1]).
    /// Supports multiple ASR models (Moonshine, Whisper) via SetModel.
    /// </summary>
    public interface ISpeechRecognitionService : IDisposable
    {
        /// <summary>Whether the required ASR models for the current model type are downloaded and available.</summary>
        bool AreModelsAvailable { get; }

        /// <summary>The currently selected ASR model type.</summary>
        ASRModelType CurrentModelType { get; }

        /// <summary>
        /// Switch to a different ASR model. Disposes the current recognizer;
        /// the new one will be lazily initialized on the next TranscribeAsync call.
        /// </summary>
        void SetModel(ASRModelType type);

        /// <summary>
        /// Transcribe audio samples to text.
        /// </summary>
        /// <param name="samples">16kHz mono float[] audio samples, normalized [-1,1].</param>
        /// <param name="sampleRate">Sample rate (expected: 16000).</param>
        /// <returns>Transcribed text, or empty string if no speech detected.</returns>
        Task<string> TranscribeAsync(float[] samples, int sampleRate = 16000);
    }
}
