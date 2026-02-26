using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MeetingNotesApp
{
    /// <summary>
    /// Manages speaker diarization model downloads, paths, and existence checks.
    /// Models are stored at %LocalAppData%/MeetingNotesApp/models/sherpa-onnx/.
    ///
    /// Two model types:
    ///   - Segmentation: User-selectable (Pyannote 3.0 or Reverb v1) via DiarizationSegmentationModelType
    ///   - Embedding: Single hardcoded model (3D-Speaker CampPlus English, ~30 MB)
    /// </summary>
    public class DiarizationModelManager
    {
        private static readonly string ModelsBaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingNotesApp", "models", "sherpa-onnx");

        // --- Embedding model (single, hardcoded — best English option at 0.65% EER) ---

        public static string EmbeddingModelPath => Path.Combine(
            ModelsBaseDir, "3dspeaker_speech_campplus_sv_en_voxceleb_16k.onnx");

        private const string EmbeddingModelUrl =
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/speaker-recongition-models/3dspeaker_speech_campplus_sv_en_voxceleb_16k.onnx";

        private const long ExpectedEmbeddingSizeBytes = 29_600_000; // ~30 MB

        public static bool IsEmbeddingModelDownloaded => File.Exists(EmbeddingModelPath);

        // --- Segmentation model (per-model, user-selectable) ---

        /// <summary>Get the directory for a specific segmentation model.</summary>
        public static string GetSegmentationModelDirectory(DiarizationSegmentationModelType type)
        {
            var def = DiarizationModelDefinition.Get(type);
            return Path.Combine(ModelsBaseDir, def.FolderName);
        }

        /// <summary>Get the full path to a segmentation model's ONNX file.</summary>
        public static string GetSegmentationModelPath(DiarizationSegmentationModelType type)
        {
            var def = DiarizationModelDefinition.Get(type);
            return Path.Combine(ModelsBaseDir, def.FolderName, def.FileName);
        }

        /// <summary>Whether a specific segmentation model is downloaded.</summary>
        public static bool IsSegmentationModelDownloaded(DiarizationSegmentationModelType type) =>
            File.Exists(GetSegmentationModelPath(type));

        /// <summary>List all downloaded segmentation model types.</summary>
        public static List<DiarizationSegmentationModelType> GetDownloadedSegmentationModels() =>
            DiarizationModelDefinition.AllModels
                .Where(m => IsSegmentationModelDownloaded(m.ModelType))
                .Select(m => m.ModelType)
                .ToList();

        /// <summary>
        /// Whether the selected segmentation model AND the embedding model are both downloaded.
        /// Used by SherpaOnnxDiarizationService to check readiness.
        /// </summary>
        public static bool AreModelsDownloaded =>
            IsSegmentationModelDownloaded(AppSettings.Diarization.SelectedSegmentationModel) &&
            IsEmbeddingModelDownloaded;

        // --- Download methods ---

        /// <summary>Download a specific segmentation model.</summary>
        public async Task DownloadSegmentationModelAsync(
            DiarizationSegmentationModelType type,
            IProgress<(double percent, string status)> progress,
            CancellationToken cancellationToken = default)
        {
            var def = DiarizationModelDefinition.Get(type);
            var dir = GetSegmentationModelDirectory(type);
            var path = GetSegmentationModelPath(type);

            Directory.CreateDirectory(dir);

            progress.Report((0, $"Downloading {def.DisplayName} ({def.SizeText})..."));

            await DownloadFileAsync(
                def.Url,
                path,
                def.ExpectedSizeBytes,
                (downloaded) =>
                {
                    var percent = (double)downloaded / def.ExpectedSizeBytes * 100;
                    var mb = downloaded / (1024.0 * 1024.0);
                    var totalMb = def.ExpectedSizeBytes / (1024.0 * 1024.0);
                    progress.Report((percent, $"Downloading {def.DisplayName}... {percent:F0}% ({mb:F1} / {totalMb:F1} MB)"));
                },
                cancellationToken);

            progress.Report((100, $"{def.DisplayName} downloaded."));
        }

        /// <summary>Download the embedding model.</summary>
        public async Task DownloadEmbeddingModelAsync(
            IProgress<(double percent, string status)> progress,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ModelsBaseDir);

            progress.Report((0, "Downloading speaker embedding model (~30 MB)..."));

            await DownloadFileAsync(
                EmbeddingModelUrl,
                EmbeddingModelPath,
                ExpectedEmbeddingSizeBytes,
                (downloaded) =>
                {
                    var percent = (double)downloaded / ExpectedEmbeddingSizeBytes * 100;
                    var mb = downloaded / (1024.0 * 1024.0);
                    progress.Report((percent, $"Downloading embedding model... {percent:F0}% ({mb:F1} / 28.2 MB)"));
                },
                cancellationToken);

            progress.Report((100, "Embedding model downloaded."));
        }

        // --- Delete methods ---

        /// <summary>Delete a specific segmentation model.</summary>
        public static void DeleteSegmentationModel(DiarizationSegmentationModelType type)
        {
            var path = GetSegmentationModelPath(type);
            var dir = GetSegmentationModelDirectory(type);

            if (File.Exists(path))
                File.Delete(path);

            if (Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0)
                Directory.Delete(dir);
        }

        /// <summary>Delete the embedding model.</summary>
        public static void DeleteEmbeddingModel()
        {
            if (File.Exists(EmbeddingModelPath))
                File.Delete(EmbeddingModelPath);
        }

        // --- Private helpers ---

        private static async Task DownloadFileAsync(
            string url,
            string destinationPath,
            long expectedSize,
            Action<long> onProgress,
            CancellationToken cancellationToken)
        {
            var tmpPath = destinationPath + ".tmp";

            try
            {
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30);

                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? expectedSize;
                long downloadedBytes = 0;
                var lastUiUpdate = DateTime.UtcNow;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920);

                var buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    downloadedBytes += bytesRead;

                    var now = DateTime.UtcNow;
                    if ((now - lastUiUpdate).TotalMilliseconds >= 100)
                    {
                        lastUiUpdate = now;
                        onProgress(downloadedBytes);
                    }
                }

                fileStream.Close();

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(tmpPath, destinationPath);
            }
            catch
            {
                if (File.Exists(tmpPath))
                {
                    try { File.Delete(tmpPath); } catch { }
                }
                throw;
            }
        }
    }
}
