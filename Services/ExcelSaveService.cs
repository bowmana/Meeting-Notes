using MeetingNotesApp.Models;

namespace MeetingNotesApp.Services
{
    public class ExcelSaveService : IMeetingSaveService
    {
        public Task SaveMeetingAsync(MeetingData data, Integration integration)
        {
            throw new NotImplementedException("Excel export is coming soon.");
        }
    }
}
