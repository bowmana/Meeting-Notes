namespace MeetingNotesApp.Models
{
    public class ExcelExportIntegration : Integration
    {
        public override IntegrationProviderType ProviderType => IntegrationProviderType.ExcelExport;
        public override string ProviderDisplayName => "Excel Export";
        public override string TargetDescription => ExportPath ?? "No path selected";
        public override string SaveButtonText => "Export to Excel";

        public string? ExportPath { get; set; }
        public bool AppendToSingleFile { get; set; } = false;
    }
}
