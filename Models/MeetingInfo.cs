namespace MeetingNotesApp.Models
{
    public class MeetingInfo
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = "";
        public string Organizer { get; set; } = "";
        public string Attendees { get; set; } = "";
        public string Comments { get; set; } = "";
        public Integration? Integration { get; set; }
        public DateTime StartTime { get; set; }
    }
}
