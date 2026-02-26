using System;
using System.Collections.Generic;
using System.Linq;

namespace MeetingNotesApp
{
    /// <summary>
    /// A single segment of speaker-tagged transcription produced by the diarization pipeline.
    /// Each segment represents one continuous speech turn by one speaker.
    /// </summary>
    public class TranscriptionSegment
    {
        /// <summary>0-indexed speaker ID from diarization clustering (0, 1, 2...)</summary>
        public int SpeakerIndex { get; set; }

        /// <summary>Display label: "Speaker 1", "Speaker 2", etc.</summary>
        public string SpeakerLabel { get; set; } = "";

        /// <summary>Transcribed text for this segment.</summary>
        public string Text { get; set; } = "";

        /// <summary>Segment start time in seconds.</summary>
        public float StartSeconds { get; set; }

        /// <summary>Segment end time in seconds.</summary>
        public float EndSeconds { get; set; }

        /// <summary>Formatted timestamp string like "[0:00 - 0:15]".</summary>
        public string TimestampDisplay =>
            $"[{FormatTime(StartSeconds)} - {FormatTime(EndSeconds)}]";

        /// <summary>Full display string: "Speaker 1 [0:00 - 0:15]: Hello everyone..."</summary>
        public string DisplayText =>
            $"{SpeakerLabel} {TimestampDisplay}: {Text}";

        private static string FormatTime(float seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    /// <summary>
    /// Complete result of a diarization + transcription pipeline run.
    /// </summary>
    public class DiarizedTranscription
    {
        public List<TranscriptionSegment> Segments { get; set; } = new();
        public int SpeakerCount { get; set; }
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Speaker-labeled text for display and Notion saving.
        /// Format: "Speaker 1 [0:00 - 0:15]: text\nSpeaker 2 [0:15 - 0:30]: text\n..."
        /// </summary>
        public string ToFlatText()
        {
            return string.Join("\n", Segments
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Select(s => s.DisplayText));
        }

        /// <summary>
        /// Plain text without speaker labels (for backward compatibility or plain-text contexts).
        /// </summary>
        public string ToPlainText()
        {
            return string.Join(" ", Segments
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Select(s => s.Text));
        }
    }
}
