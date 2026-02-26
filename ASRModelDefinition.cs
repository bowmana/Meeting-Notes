using System.Linq;

namespace MeetingNotesApp
{
    /// <summary>
    /// Available ASR model types. Each model can be independently downloaded and selected.
    /// All models run via sherpa-onnx OfflineRecognizer — no additional NuGet packages needed.
    /// </summary>
    public enum ASRModelType
    {
        MoonshineTiny,
        MoonshineBase,
        WhisperTinyEn,
        WhisperBaseEn,
        WhisperSmallEn
    }

    /// <summary>
    /// Describes an ASR model: display info, download URLs, file sizes, and config type.
    /// Used by ASRModelManager for downloads and SherpaOnnxASRService for recognizer config.
    /// </summary>
    public class ASRModelDefinition
    {
        public ASRModelType ModelType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string FolderName { get; }
        public bool IsMoonshine { get; }
        public (string fileName, string url, long expectedSize)[] Files { get; }

        public long TotalSizeBytes => Files.Sum(f => f.expectedSize);
        public string SizeText => TotalSizeBytes >= 1_000_000_000
            ? $"{TotalSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
            : $"{TotalSizeBytes / (1024.0 * 1024.0):F0} MB";

        private ASRModelDefinition(
            ASRModelType modelType, string displayName, string description,
            string folderName, bool isMoonshine,
            (string fileName, string url, long expectedSize)[] files)
        {
            ModelType = modelType;
            DisplayName = displayName;
            Description = description;
            FolderName = folderName;
            IsMoonshine = isMoonshine;
            Files = files;
        }

        // --- Moonshine HuggingFace base URLs ---
        private const string MoonshineTinyBase = "https://huggingface.co/csukuangfj/sherpa-onnx-moonshine-tiny-en-int8/resolve/main/";
        private const string MoonshineBaseBase = "https://huggingface.co/csukuangfj/sherpa-onnx-moonshine-base-en-int8/resolve/main/";

        // --- Whisper HuggingFace base URLs ---
        private const string WhisperTinyEnBase = "https://huggingface.co/csukuangfj/sherpa-onnx-whisper-tiny.en/resolve/main/";
        private const string WhisperBaseEnBase = "https://huggingface.co/csukuangfj/sherpa-onnx-whisper-base.en/resolve/main/";
        private const string WhisperSmallEnBase = "https://huggingface.co/csukuangfj/sherpa-onnx-whisper-small.en/resolve/main/";

        /// <summary>All available ASR models.</summary>
        public static readonly ASRModelDefinition[] AllModels = new[]
        {
            new ASRModelDefinition(
                ASRModelType.MoonshineTiny,
                "Moonshine Tiny (int8)",
                "Fastest. English only. ~12% WER.",
                "moonshine-tiny-int8",
                isMoonshine: true,
                new (string, string, long)[]
                {
                    ("preprocess.onnx",          MoonshineTinyBase + "preprocess.onnx",          7_100_000),
                    ("encode.int8.onnx",         MoonshineTinyBase + "encode.int8.onnx",         19_000_000),
                    ("cached_decode.int8.onnx",  MoonshineTinyBase + "cached_decode.int8.onnx",  47_500_000),
                    ("uncached_decode.int8.onnx", MoonshineTinyBase + "uncached_decode.int8.onnx", 55_800_000),
                    ("tokens.txt",               MoonshineTinyBase + "tokens.txt",               450_000),
                }),

            new ASRModelDefinition(
                ASRModelType.MoonshineBase,
                "Moonshine Base (int8)",
                "Better accuracy. English only. ~9% WER.",
                "moonshine-base-int8",
                isMoonshine: true,
                new (string, string, long)[]
                {
                    ("preprocess.onnx",          MoonshineBaseBase + "preprocess.onnx",          14_800_000),
                    ("encode.int8.onnx",         MoonshineBaseBase + "encode.int8.onnx",         52_700_000),
                    ("cached_decode.int8.onnx",  MoonshineBaseBase + "cached_decode.int8.onnx",  104_800_000),
                    ("uncached_decode.int8.onnx", MoonshineBaseBase + "uncached_decode.int8.onnx", 127_900_000),
                    ("tokens.txt",               MoonshineBaseBase + "tokens.txt",               450_000),
                }),

            new ASRModelDefinition(
                ASRModelType.WhisperTinyEn,
                "Whisper tiny.en (int8)",
                "OpenAI Whisper. English only. ~12% WER.",
                "whisper-tiny-en-int8",
                isMoonshine: false,
                new (string, string, long)[]
                {
                    ("tiny.en-encoder.int8.onnx", WhisperTinyEnBase + "tiny.en-encoder.int8.onnx", 13_500_000),
                    ("tiny.en-decoder.int8.onnx", WhisperTinyEnBase + "tiny.en-decoder.int8.onnx", 94_200_000),
                    ("tiny.en-tokens.txt",        WhisperTinyEnBase + "tiny.en-tokens.txt",        860_000),
                }),

            new ASRModelDefinition(
                ASRModelType.WhisperBaseEn,
                "Whisper base.en (int8)",
                "OpenAI Whisper. Good quality. ~10% WER.",
                "whisper-base-en-int8",
                isMoonshine: false,
                new (string, string, long)[]
                {
                    ("base.en-encoder.int8.onnx", WhisperBaseEnBase + "base.en-encoder.int8.onnx", 30_500_000),
                    ("base.en-decoder.int8.onnx", WhisperBaseEnBase + "base.en-decoder.int8.onnx", 137_300_000),
                    ("base.en-tokens.txt",        WhisperBaseEnBase + "base.en-tokens.txt",        860_000),
                }),

            new ASRModelDefinition(
                ASRModelType.WhisperSmallEn,
                "Whisper small.en (int8) (Recommended)",
                "Best quality. English only. ~5% WER.",
                "whisper-small-en-int8",
                isMoonshine: false,
                new (string, string, long)[]
                {
                    ("small.en-encoder.int8.onnx", WhisperSmallEnBase + "small.en-encoder.int8.onnx", 117_400_000),
                    ("small.en-decoder.int8.onnx", WhisperSmallEnBase + "small.en-decoder.int8.onnx", 274_700_000),
                    ("small.en-tokens.txt",        WhisperSmallEnBase + "small.en-tokens.txt",        860_000),
                }),
        };

        /// <summary>Get the definition for a specific model type.</summary>
        public static ASRModelDefinition Get(ASRModelType type) =>
            AllModels.First(m => m.ModelType == type);
    }
}
