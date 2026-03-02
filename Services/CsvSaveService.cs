using MeetingNotesApp.Models;

namespace MeetingNotesApp.Services
{
    public class CsvSaveService : IMeetingSaveService
    {
        public Task SaveMeetingAsync(MeetingData data, Integration integration)
        {
            throw new NotImplementedException("CSV export is coming soon.");
        }
    }
}
