namespace MeetingNotesApp.Models
{
    /// <summary>
    /// Flat JSON-safe structure with nullable provider-specific fields.
    /// The ProviderType string acts as a discriminator for deserialization.
    /// Persisted to integrations.json.
    /// </summary>
    public class SerializableIntegration
    {
        public string ProviderType { get; set; } = "";
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string StatusText { get; set; } = "";

        // Notion-specific (null for other types)
        public string? ApiKey { get; set; }
        public NotionDatabase? SelectedDatabase { get; set; }
        public List<NotionDatabase>? Databases { get; set; }

        // CSV/Excel-specific (null for Notion)
        public string? ExportPath { get; set; }
        public bool? AppendToSingleFile { get; set; }
    }
}
