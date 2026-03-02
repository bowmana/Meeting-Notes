using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeetingNotesApp.Models;

namespace MeetingNotesApp.Services
{
    public class NotionSaveService : IMeetingSaveService
    {
        public async Task SaveMeetingAsync(MeetingData data, Integration integration)
        {
            if (integration is not NotionIntegration notionIntegration)
                throw new ArgumentException("NotionSaveService requires a NotionIntegration.");

            var database = notionIntegration.SelectedDatabase
                ?? throw new InvalidOperationException("No database selected for this Notion integration.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", notionIntegration.ApiKey);
            httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            // Format key points as bullet list
            var keyPointsText = string.Join("\n", data.KeyPoints.Select(kp => $"• {kp}"));

            // Format action items as bullet list with assignees
            var actionItemsText = string.Join("\n", data.ActionItems.Select(ai => $"• {ai.Text} (Assigned to: {ai.Assignee})"));

            var properties = new Dictionary<string, object>
            {
                ["Title"] = new
                {
                    title = new[]
                    {
                        new { text = new { content = data.Title } }
                    }
                },
                ["Transcription"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = TruncateForNotion(data.Transcription) } }
                    }
                },
                ["My Notes"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.ManualNotes } }
                    }
                },
                ["AI Summary"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.AiSummary } }
                    }
                },
                ["Key Points"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = keyPointsText } }
                    }
                },
                ["Action Items"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = actionItemsText } }
                    }
                },
                ["Duration"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.Duration } }
                    }
                },
                ["Organizer"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.Organizer } }
                    }
                },
                ["Attendees"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.Attendees } }
                    }
                }
            };

            // Add Speakers property if diarization was used
            if (data.SpeakerCount > 0 && !string.IsNullOrEmpty(data.SpeakersDescription))
            {
                properties["Speakers"] = new
                {
                    rich_text = new[]
                    {
                        new { text = new { content = data.SpeakersDescription } }
                    }
                };
            }

            // Add Date if available
            if (data.Date != default)
            {
                properties["Date"] = new
                {
                    date = new { start = data.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                };
            }

            var requestBody = new
            {
                parent = new { database_id = database.Id },
                properties = properties
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.notion.com/v1/pages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Notion API error: {response.StatusCode} - {errorContent}");
            }
        }

        private static string TruncateForNotion(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= 2000) return text;
            return text.Substring(0, 1990) + "\n... (truncated)";
        }
    }
}
