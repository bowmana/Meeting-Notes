using System.Collections.ObjectModel;

namespace MeetingNotesApp.Models
{
    public class NotionIntegration : Integration
    {
        public override IntegrationProviderType ProviderType => IntegrationProviderType.Notion;
        public override string ProviderDisplayName => "Notion";
        public override string TargetDescription => SelectedDatabase?.Name ?? "No database selected";
        public override string SaveButtonText => "Save to Notion";

        public string ApiKey { get; set; } = "";
        public NotionDatabase? SelectedDatabase { get; set; }
        public ObservableCollection<NotionDatabase> Databases { get; set; } = new();
    }
}
