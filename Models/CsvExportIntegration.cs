namespace MeetingNotesApp.Models
{
    public class CsvExportIntegration : Integration
    {
        public override IntegrationProviderType ProviderType => IntegrationProviderType.CsvExport;
        public override string ProviderDisplayName => "CSV Export";
        public override string TargetDescription => ExportFolderPath ?? "No folder selected";
        public override string SaveButtonText => "Export to CSV";

        public string? ExportFolderPath { get; set; }
    }
}
