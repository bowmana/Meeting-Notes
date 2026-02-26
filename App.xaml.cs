using System.Windows;

namespace MeetingNotesApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Register crash logging first, before anything else can fail
            var crashLog = new CrashLogService();
            crashLog.RegisterGlobalExceptionHandlers();
        }
    }
}
