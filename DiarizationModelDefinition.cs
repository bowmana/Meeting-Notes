using System.Linq;

namespace MeetingNotesApp
{
    /// <summary>
    /// Available diarization segmentation model types.
    /// Each model can be independently downloaded and selected.
    /// All models use the same sherpa-onnx Pyannote.Model config path — drop-in replacements.
    /// </summary>
    public enum DiarizationSegmentationModelType
    {
        Pyannote3,      // pyannote-segmentation-3.0 (baseline)
        ReverbV1        // reverb-diarization-v1 (fine-tuned on 26K hours of Rev.com data)
    }

    /// <summary>
    /// Describes a diarization segmentation model: display info, download URL, file size.
    /// Used by DiarizationModelManager for downloads and SherpaOnnxDiarizationService for config.
    /// </summary>
    public class DiarizationModelDefinition
    {
        public DiarizationSegmentationModelType ModelType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string FolderName { get; }
        public string FileName { get; }
        public string Url { get; }
        public long ExpectedSizeBytes { get; }

        public string SizeText => ExpectedSizeBytes >= 1_000_000_000
            ? $"{ExpectedSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
            : $"{ExpectedSizeBytes / (1024.0 * 1024.0):F0} MB";

        private DiarizationModelDefinition(
            DiarizationSegmentationModelType modelType, string displayName, string description,
            string folderName, string fileName, string url, long expectedSizeBytes)
        {
            ModelType = modelType;
            DisplayName = displayName;
            Description = description;
            FolderName = folderName;
            FileName = fileName;
            Url = url;
            ExpectedSizeBytes = expectedSizeBytes;
        }

        /// <summary>All available segmentation models.</summary>
        public static readonly DiarizationModelDefinition[] AllModels = new[]
        {
            new DiarizationModelDefinition(
                DiarizationSegmentationModelType.Pyannote3,
                "Pyannote 3.0",
                "General-purpose baseline segmentation model.",
                "sherpa-onnx-pyannote-segmentation-3-0",
                "model.onnx",
                "https://huggingface.co/csukuangfj/sherpa-onnx-pyannote-segmentation-3-0/resolve/main/model.onnx",
                6_300_000),

            new DiarizationModelDefinition(
                DiarizationSegmentationModelType.ReverbV1,
                "Reverb Diarization v1 (Recommended)",
                "Fine-tuned on 26K hours of call data. ~16% more accurate.",
                "sherpa-onnx-reverb-diarization-v1",
                "model.onnx",
                "https://huggingface.co/csukuangfj/sherpa-onnx-reverb-diarization-v1/resolve/main/model.onnx",
                9_510_000),
        };

        /// <summary>Get the definition for a specific model type.</summary>
        public static DiarizationModelDefinition Get(DiarizationSegmentationModelType type) =>
            AllModels.First(m => m.ModelType == type);
    }
}
