using System;
using System.Threading.Tasks;
using SherpaOnnx;

namespace MeetingNotesApp
{
    /// <summary>
    /// Speech recognition service using sherpa-onnx's OfflineRecognizer.
    /// Supports both Moonshine and Whisper model families via ASRModelType.
    /// The OfflineRecognizer is lazily initialized and cached — it's expensive to create (~2-5s)
    /// but fast for subsequent transcriptions. Calling SetModel disposes the old recognizer
    /// so a new one is created on the next TranscribeAsync call.
    /// </summary>
    public class SherpaOnnxASRService : ISpeechRecognitionService
    {
        private OfflineRecognizer? _recognizer;
        private bool _disposed;
        private readonly object _initLock = new();
        private ASRModelType _currentModelType;

        public SherpaOnnxASRService(ASRModelType modelType = ASRModelType.MoonshineTiny)
        {
            _currentModelType = modelType;
        }

        public bool AreModelsAvailable => ASRModelManager.AreModelsDownloaded(_currentModelType);

        public ASRModelType CurrentModelType => _currentModelType;

        public void SetModel(ASRModelType type)
        {
            if (_currentModelType == type)
                return;

            lock (_initLock)
            {
                _recognizer?.Dispose();
                _recognizer = null;
                _currentModelType = type;
            }
        }

        /// <summary>
        /// Lazily initializes the OfflineRecognizer on first use.
        /// Configures either Moonshine or Whisper based on the current model type.
        /// </summary>
        private OfflineRecognizer GetOrCreateRecognizer()
        {
            if (_recognizer == null)
            {
                lock (_initLock)
                {
                    if (_recognizer == null)
                    {
                        if (!AreModelsAvailable)
                            throw new InvalidOperationException(
                                $"ASR models for {ASRModelDefinition.Get(_currentModelType).DisplayName} are not downloaded. " +
                                "Go to Settings → Speech Recognition to download them.");

                        var def = ASRModelDefinition.Get(_currentModelType);
                        var config = new OfflineRecognizerConfig();

                        // IMPORTANT: OfflineRecognizerConfig and all nested types (OfflineModelConfig,
                        // OfflineWhisperModelConfig, OfflineMoonshineModelConfig) are STRUCTS with
                        // [StructLayout(LayoutKind.Sequential)] for P/Invoke marshaling. You CANNOT
                        // chain property access like config.ModelConfig.Tokens = "..." because each
                        // property access returns a COPY of the struct. You must copy to a local,
                        // modify, then assign back.
                        var modelConfig = config.ModelConfig;

                        if (def.IsMoonshine)
                        {
                            var moonshine = modelConfig.Moonshine;
                            moonshine.Preprocessor = ASRModelManager.GetModelFilePath(def.ModelType, "preprocess.onnx");
                            moonshine.Encoder = ASRModelManager.GetModelFilePath(def.ModelType, "encode.int8.onnx");
                            moonshine.UncachedDecoder = ASRModelManager.GetModelFilePath(def.ModelType, "uncached_decode.int8.onnx");
                            moonshine.CachedDecoder = ASRModelManager.GetModelFilePath(def.ModelType, "cached_decode.int8.onnx");
                            modelConfig.Moonshine = moonshine;
                            modelConfig.Tokens = ASRModelManager.GetModelFilePath(def.ModelType, "tokens.txt");
                        }
                        else
                        {
                            var encoderFile = FindFile(def, "encoder");
                            var decoderFile = FindFile(def, "decoder");
                            var tokensFile = FindFile(def, "tokens");

                            var whisper = modelConfig.Whisper;
                            whisper.Encoder = ASRModelManager.GetModelFilePath(def.ModelType, encoderFile);
                            whisper.Decoder = ASRModelManager.GetModelFilePath(def.ModelType, decoderFile);
                            whisper.Language = "en";
                            whisper.Task = "transcribe";
                            whisper.TailPaddings = -1;
                            modelConfig.Whisper = whisper;
                            modelConfig.Tokens = ASRModelManager.GetModelFilePath(def.ModelType, tokensFile);
                        }

                        modelConfig.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
                        modelConfig.Provider = "cpu";
                        config.ModelConfig = modelConfig;

                        _recognizer = new OfflineRecognizer(config);
                    }
                }
            }
            return _recognizer;
        }

        /// <summary>Find a file in the model definition by partial name match.</summary>
        private static string FindFile(ASRModelDefinition def, string keyword)
        {
            foreach (var (fileName, _, _) in def.Files)
            {
                if (fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return fileName;
            }
            throw new InvalidOperationException($"No file matching '{keyword}' found in {def.DisplayName} model definition.");
        }

        public async Task<string> TranscribeAsync(float[] samples, int sampleRate = 16000)
        {
            if (samples == null || samples.Length == 0)
                return "";

            return await Task.Run(() =>
            {
                var recognizer = GetOrCreateRecognizer();
                var stream = recognizer.CreateStream();
                stream.AcceptWaveform(sampleRate, samples);
                recognizer.Decode(stream);
                return stream.Result.Text?.Trim() ?? "";
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _recognizer?.Dispose();
                _recognizer = null;
                _disposed = true;
            }
        }
    }
}
