using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace MeetingNotesApp
{
    /// <summary>
    /// Shared audio utility methods used by diarization and transcription services.
    /// </summary>
    public static class AudioHelper
    {
        /// <summary>
        /// Loads a WAV file as float[] normalized to [-1.0, 1.0].
        /// Resamples to 16kHz mono if needed (the app's conversion pipeline should already produce this).
        /// </summary>
        public static float[] LoadWavAsFloats(string wavFilePath)
        {
            using var reader = new AudioFileReader(wavFilePath);

            // Check if resampling is needed
            ISampleProvider sampleProvider;
            if (reader.WaveFormat.SampleRate != 16000 || reader.WaveFormat.Channels != 1)
            {
                var targetFormat = new WaveFormat(16000, 16, 1);
                var resampler = new MediaFoundationResampler(reader, targetFormat);
                resampler.ResamplerQuality = 60;
                sampleProvider = resampler.ToSampleProvider();
            }
            else
            {
                sampleProvider = reader;
            }

            // Read all samples
            var allSamples = new List<float>();
            var buffer = new float[16000]; // 1 second at 16kHz
            int read;

            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    // Clamp to [-1, 1] (should already be normalized by AudioFileReader)
                    allSamples.Add(Math.Clamp(buffer[i], -1.0f, 1.0f));
                }
            }

            return allSamples.ToArray();
        }
    }
}
