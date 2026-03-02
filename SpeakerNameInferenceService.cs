using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace MeetingNotesApp
{
    public class SpeakerNameInference
    {
        public int SpeakerIndex { get; set; }
        public string InferredName { get; set; } = "";
        public float Confidence { get; set; }
        public string Evidence { get; set; } = "";
    }

    public interface ISpeakerNameInferenceService : IDisposable
    {
        bool IsModelAvailable { get; }
        Task<List<SpeakerNameInference>> InferSpeakerNamesAsync(
            DiarizedTranscription transcription,
            CancellationToken ct = default);
    }

    public class SpeakerNameInferenceService : ISpeakerNameInferenceService
    {
        private static readonly string ModelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingNotesApp", "models", "Phi-4-mini-instruct-Q4_K_M.gguf");

        private LLamaWeights? _model;
        private bool _disposed;

        public bool IsModelAvailable => File.Exists(ModelPath);

        public async Task<List<SpeakerNameInference>> InferSpeakerNamesAsync(
            DiarizedTranscription transcription,
            CancellationToken ct = default)
        {
            if (transcription == null || transcription.Segments.Count == 0)
                return new List<SpeakerNameInference>();

            if (!IsModelAvailable)
                return new List<SpeakerNameInference>();

            // Load model if not already loaded
            if (_model == null)
            {
                var modelParams = new ModelParams(ModelPath)
                {
                    ContextSize = 2048,
                    GpuLayerCount = -1
                };
                _model = await Task.Run(() => LLamaWeights.LoadFromFile(modelParams), ct);
            }

            var prompt = BuildPrompt(transcription);
            var output = await RunInferenceAsync(prompt, ct);
            return ParseInferenceOutput(output, transcription.SpeakerCount);
        }

        private static string BuildPrompt(DiarizedTranscription transcription)
        {
            // Build a concise transcript excerpt (limit to ~1500 chars to fit in context)
            var transcriptText = new StringBuilder();
            foreach (var seg in transcription.Segments.Where(s => !string.IsNullOrWhiteSpace(s.Text)))
            {
                var line = $"{seg.SpeakerLabel}: {seg.Text}";
                if (transcriptText.Length + line.Length > 1500)
                    break;
                transcriptText.AppendLine(line);
            }

            var exampleJson = "{\"speakers\":[{\"index\":0,\"name\":\"Alice\",\"confidence\":0.95,\"evidence\":\"said I'm Alice\"},{\"index\":1,\"name\":null,\"confidence\":0.0,\"evidence\":\"no name mentioned\"}]}";

            var sb = new StringBuilder();
            sb.AppendLine("<|system|>");
            sb.AppendLine("You identify real human names from meeting transcripts.");
            sb.AppendLine();
            sb.AppendLine("CRITICAL RULES:");
            sb.AppendLine("- NEVER return speaker labels as names. \"Speaker 1\", \"Speaker 2\", \"Person 3\", \"Participant\" etc. are NOT names.");
            sb.AppendLine("- A name MUST be a real human name (first name, last name, or nickname) explicitly mentioned in the conversation.");
            sb.AppendLine("- If you cannot find a speaker's real name in the text, you MUST return null for that speaker.");
            sb.AppendLine("- Only assign a name when there is clear, direct evidence in the transcript text.");
            sb.AppendLine("- Look for: greetings (\"Hey Tom\"), introductions (\"I'm Alice\"), references (\"Thanks Bob\").");
            sb.AppendLine("- Return valid JSON only, no other text.");
            sb.AppendLine("- Most speakers will NOT have identifiable names — returning null is the correct default.");
            sb.AppendLine();
            sb.AppendLine("Example input:");
            sb.AppendLine("Speaker 1: Hi everyone, I'm Alice from Product.");
            sb.AppendLine("Speaker 2: Sounds good, let me share my screen.");
            sb.AppendLine("Speaker 1: Great.");
            sb.AppendLine();
            sb.AppendLine("Example output (Speaker 2 has no name mentioned, so null):");
            sb.AppendLine(exampleJson);
            sb.AppendLine("<|end|>");
            sb.AppendLine("<|user|>");
            sb.AppendLine("Identify speaker names from this transcript:");
            sb.AppendLine();
            sb.Append(transcriptText);
            sb.AppendLine();
            sb.AppendLine("Return JSON only.");
            sb.AppendLine("<|end|>");
            sb.AppendLine("<|assistant|>");

            return sb.ToString();
        }

        private async Task<string> RunInferenceAsync(string prompt, CancellationToken ct)
        {
            var executorParams = new ModelParams(ModelPath)
            {
                ContextSize = 2048
            };

            var executor = new StatelessExecutor(_model!, executorParams);

            var samplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.1f,
                Seed = 42
            };

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 300,
                AntiPrompts = new List<string> { "<|end|>", "\n\n" },
                SamplingPipeline = samplingPipeline
            };

            var output = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
            {
                output.Append(token);
            }

            return output.ToString().Trim();
        }

        private static List<SpeakerNameInference> ParseInferenceOutput(string output, int speakerCount)
        {
            var results = new List<SpeakerNameInference>();

            try
            {
                // Find JSON in the output (LLM may include extra text)
                var jsonStart = output.IndexOf('{');
                var jsonEnd = output.LastIndexOf('}');
                if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
                    return results;

                var json = output.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("speakers", out var speakers))
                    return results;

                foreach (var speaker in speakers.EnumerateArray())
                {
                    var index = speaker.GetProperty("index").GetInt32();
                    var nameElement = speaker.GetProperty("name");

                    // Skip null names
                    if (nameElement.ValueKind == JsonValueKind.Null)
                        continue;

                    var name = nameElement.GetString();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    // Reject speaker labels hallucinated as names
                    if (IsGenericSpeakerLabel(name))
                        continue;

                    var confidence = speaker.TryGetProperty("confidence", out var conf)
                        ? conf.GetSingle()
                        : 0.5f;

                    var evidence = speaker.TryGetProperty("evidence", out var ev)
                        ? ev.GetString() ?? ""
                        : "";

                    // Only accept inferences above the confidence threshold
                    if (confidence >= 0.7f && index >= 0 && index < speakerCount)
                    {
                        results.Add(new SpeakerNameInference
                        {
                            SpeakerIndex = index,
                            InferredName = name,
                            Confidence = confidence,
                            Evidence = evidence
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // LLM output wasn't valid JSON — return empty results
            }

            return results;
        }

        private static bool IsGenericSpeakerLabel(string name)
        {
            var trimmed = name.Trim();

            // Reject: "Speaker 1", "Speaker 12", "Person 3", "Participant 2", etc.
            if (Regex.IsMatch(trimmed,
                @"^(speaker|person|participant|user|attendee|unknown)\s*\d*$",
                RegexOptions.IgnoreCase))
                return true;

            // Reject: pure numeric "names"
            if (int.TryParse(trimmed, out _))
                return true;

            // Reject: very short names (1 char)
            if (trimmed.Length <= 1)
                return true;

            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _model?.Dispose();
                _model = null;
                _disposed = true;
            }
        }
    }
}
