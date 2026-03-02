using MeetingNotesApp.Models;

namespace MeetingNotesApp.Services
{
    public class MeetingData
    {
        public string Title { get; set; } = "";
        public string Transcription { get; set; } = "";
        public string ManualNotes { get; set; } = "";
        public string AiSummary { get; set; } = "";
        public List<string> KeyPoints { get; set; } = new();
        public List<(string Text, string Assignee)> ActionItems { get; set; } = new();
        public string Duration { get; set; } = "";
        public string Organizer { get; set; } = "";
        public string Attendees { get; set; } = "";
        public DateTime Date { get; set; }
        public int SpeakerCount { get; set; }
        public string SpeakersDescription { get; set; } = "";
    }

    public interface IMeetingSaveService
    {
        Task SaveMeetingAsync(MeetingData data, Integration integration);
    }
}
