using System.Windows.Media;

namespace MeetingNotesApp.Models
{
    public enum IntegrationProviderType
    {
        Notion,
        GoogleDrive,
        OneDrive,
        Confluence,
        Slack,
        CsvExport,
        ExcelExport,
        MarkdownExport,
        PdfExport,
        Webhook
    }

    public abstract class Integration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
        public string DisplayName { get; set; } = "";
        public abstract IntegrationProviderType ProviderType { get; }
        public string StatusText { get; set; } = "Connected";

        // Runtime-only (not serialized)
        public Brush StatusColor { get; set; } = Brushes.Green;

        // Provider-specific display (abstract, implemented by subclasses)
        public abstract string ProviderDisplayName { get; }
        public abstract string TargetDescription { get; }
        public abstract string SaveButtonText { get; }
    }
}
