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

        /// <summary>Default label: "Speaker 1", "Speaker 2", etc.</summary>
        public string SpeakerLabel { get; set; } = "";

        /// <summary>User-assigned or AI-inferred name (null = use default SpeakerLabel).</summary>
        public string? CustomSpeakerName { get; set; }

        /// <summary>Returns the custom name if set, otherwise the default speaker label.</summary>
        public string EffectiveSpeakerName => CustomSpeakerName ?? SpeakerLabel;

        /// <summary>Transcribed text for this segment.</summary>
        public string Text { get; set; } = "";

        /// <summary>Segment start time in seconds.</summary>
        public float StartSeconds { get; set; }

        /// <summary>Segment end time in seconds.</summary>
        public float EndSeconds { get; set; }

        /// <summary>Formatted timestamp string like "[0:00 - 0:15]".</summary>
        public string TimestampDisplay =>
            $"[{FormatTime(StartSeconds)} - {FormatTime(EndSeconds)}]";

        /// <summary>Full display string: "Tom [0:00 - 0:15]: Hello everyone..."</summary>
        public string DisplayText =>
            $"{EffectiveSpeakerName} {TimestampDisplay}: {Text}";

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

        /// <summary>Maps SpeakerIndex → custom name. Applied to all segments for that speaker.</summary>
        public Dictionary<int, string> SpeakerNames { get; set; } = new();

        /// <summary>
        /// Sets a custom name for all segments belonging to the given speaker index.
        /// </summary>
        public void SetSpeakerName(int speakerIndex, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                SpeakerNames.Remove(speakerIndex);
                foreach (var seg in Segments.Where(s => s.SpeakerIndex == speakerIndex))
                    seg.CustomSpeakerName = null;
            }
            else
            {
                SpeakerNames[speakerIndex] = name;
                foreach (var seg in Segments.Where(s => s.SpeakerIndex == speakerIndex))
                    seg.CustomSpeakerName = name;
            }
        }

        /// <summary>
        /// Returns a comma-separated list of all named speakers (for Notion Attendees field).
        /// Only includes speakers that have been given custom names.
        /// </summary>
        public string GetNamedAttendees()
        {
            return string.Join(", ", SpeakerNames
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value));
        }

        /// <summary>
        /// Returns all distinct speaker indices in the order they first appear.
        /// </summary>
        public List<int> GetSpeakerIndices()
        {
            return Segments
                .Select(s => s.SpeakerIndex)
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }

        /// <summary>
        /// Speaker-labeled text for display and Notion saving.
        /// Uses effective names (custom name if set, otherwise default label).
        /// Format: "Tom [0:00 - 0:15]: text\nSpeaker 2 [0:15 - 0:30]: text\n..."
        /// </summary>
        public string ToFlatText()
        {
            return string.Join("\n", Segments
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Select(s => s.DisplayText));
        }

        /// <summary>
        /// Plain text without speaker labels.
        /// </summary>
        public string ToPlainText()
        {
            return string.Join(" ", Segments
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .Select(s => s.Text));
        }
    }
}
