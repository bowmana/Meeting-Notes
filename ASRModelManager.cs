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
    /// Manages ASR model downloads, paths, and existence checks for all supported models.
    /// Models are stored at %LocalAppData%/MeetingNotesApp/models/sherpa-onnx/{folderName}/.
    /// Supports multiple models (Moonshine Tiny/Base, Whisper tiny.en/base.en/small.en).
    /// </summary>
    public class ASRModelManager
    {
        private static readonly string ModelsBaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeetingNotesApp", "models", "sherpa-onnx");

        /// <summary>Get the directory where a specific model's files are stored.</summary>
        public static string GetModelDirectory(ASRModelType type)
        {
            var def = ASRModelDefinition.Get(type);
            return Path.Combine(ModelsBaseDir, def.FolderName);
        }

        /// <summary>Get the full path to a specific file within a model's directory.</summary>
        public static string GetModelFilePath(ASRModelType type, string fileName)
        {
            return Path.Combine(GetModelDirectory(type), fileName);
        }

        /// <summary>Whether all required files for a specific model are downloaded.</summary>
        public static bool AreModelsDownloaded(ASRModelType type)
        {
            var def = ASRModelDefinition.Get(type);
            return def.Files.All(f => File.Exists(GetModelFilePath(type, f.fileName)));
        }

        /// <summary>Returns all model types that are currently downloaded.</summary>
        public static List<ASRModelType> GetDownloadedModels()
        {
            return ASRModelDefinition.AllModels
                .Where(m => AreModelsDownloaded(m.ModelType))
                .Select(m => m.ModelType)
                .ToList();
        }

        /// <summary>
        /// Downloads all files for a specific model with progress reporting.
        /// HTTP download → .tmp file → rename on completion.
        /// </summary>
        public async Task DownloadModelsAsync(
            ASRModelType type,
            IProgress<(double percent, string status)> progress,
            CancellationToken cancellationToken = default)
        {
            var def = ASRModelDefinition.Get(type);
            var modelDir = GetModelDirectory(type);
            Directory.CreateDirectory(modelDir);

            long totalDownloaded = 0;

            // Account for already-downloaded files
            foreach (var (fileName, url, expectedSize) in def.Files)
            {
                var filePath = Path.Combine(modelDir, fileName);
                if (File.Exists(filePath))
                    totalDownloaded += new FileInfo(filePath).Length;
            }

            foreach (var (fileName, url, expectedSize) in def.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = Path.Combine(modelDir, fileName);
                if (File.Exists(filePath))
                    continue;

                var baseDownloaded = totalDownloaded;
                progress.Report((
                    (double)baseDownloaded / def.TotalSizeBytes * 100,
                    $"Downloading {fileName}..."));

                await DownloadFileAsync(
                    url,
                    filePath,
                    (downloaded) =>
                    {
                        var current = baseDownloaded + downloaded;
                        var percent = (double)current / def.TotalSizeBytes * 100;
                        var mb = current / (1024.0 * 1024.0);
                        var totalMb = def.TotalSizeBytes / (1024.0 * 1024.0);
                        progress.Report((percent, $"Downloading {def.DisplayName}... {percent:F0}% ({mb:F1} / {totalMb:F0} MB)"));
                    },
                    cancellationToken);

                totalDownloaded = baseDownloaded + expectedSize;
            }

            progress.Report((100, $"{def.DisplayName} download complete."));
        }

        /// <summary>Downloads a single file with progress reporting, using a .tmp file during download.</summary>
        private static async Task DownloadFileAsync(
            string url,
            string destinationPath,
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

        /// <summary>Deletes all downloaded files for a specific model.</summary>
        public static void DeleteModels(ASRModelType type)
        {
            var def = ASRModelDefinition.Get(type);
            var modelDir = GetModelDirectory(type);

            foreach (var (fileName, _, _) in def.Files)
            {
                var filePath = Path.Combine(modelDir, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            // Clean up empty directory
            if (Directory.Exists(modelDir) && Directory.GetFiles(modelDir).Length == 0)
                Directory.Delete(modelDir);
        }
    }
}
