# Meeting Notes App Lessons Learned

> This file tracks bugs, failed approaches, and coding patterns that did NOT work during development.
> The purpose is to avoid repeating the same mistakes. Never delete entries — this is a permanent history.
>
> **Format per entry:**
> ```
> ### Short description of the issue
> **Date:** YYYY-MM-DD
> **Context:** What we were trying to do
> **What went wrong:** What happened / why it failed
> **Fix / Lesson:** What we did instead / what to do going forward
> ```

---

### LMStudio as external dependency created too much user friction
**Date:** 2026-02-23
**Context:** v0.1 used LMStudio (a separate desktop app) as the AI summarization backend. The app called LMStudio's local HTTP API at `http://127.0.0.1:1234/v1/chat/completions`.
**What went wrong:** Requiring users to install a separate ~400 MB app (LMStudio), manually download a ~4-5 GB model, navigate to the Server tab, and click "Start Server" before the AI feature works is unacceptable UX for a consumer app. Non-technical users would never complete this setup. Additionally, the `SummarizeTextFallback()` method silently degraded to a "first 3 sentences" truncation when LMStudio wasn't running — giving users garbage output with no indication that the real AI wasn't working.
**Fix / Lesson:** Replaced LMStudio with LLamaSharp (in-process .NET binding for llama.cpp) for Private Mode — model loads directly in the app, zero external dependencies. Added API Key Mode as an alternative for users who want cloud-powered summaries. **Never add silent fallback logic.** If the AI provider fails, show a clear error — don't silently degrade to a worse experience.

### sherpa-onnx speaker diarization is offline-only (no real-time)
**Date:** 2026-02-24
**Context:** Investigated sherpa-onnx for adding speaker diarization (identifying who said what) to the meeting notes app. The C# API provides `OfflineSpeakerDiarization` which processes complete audio buffers. We considered real-time diarization but the library only supports offline processing.
**What went wrong:** N/A — this is a design constraint of the library, not a bug. The pyannote segmentation model processes audio in 10-second chunks, and spectral clustering requires all speaker embeddings to be available before determining consistent speaker labels. Real-time diarization would produce inconsistent labels (speaker_0 in chunk 1 might be speaker_1 in chunk 2).
**Fix / Lesson:** Design the UX around post-recording processing: after the user clicks Stop Recording, show a progress bar while diarization runs on the full audio. For a 30-minute meeting, expect ~30-120 seconds of processing on CPU. The progress callback (`ProcessWithCallback`) reports chunk-by-chunk progress which maps well to a percentage-based progress bar. **Do not attempt to stream diarization results in real-time with sherpa-onnx — it will not produce correct speaker labels.**

### NAudio RecordingStopped event causes double processing if StopRecording also calls handler directly
**Date:** 2026-02-24
**Context:** During diarization pipeline integration, `StopRecording()` in NoteTakingWindow called `ProcessCapturedAudio()` directly AND `_loopbackCapture.StopRecording()` fired the `RecordingStopped` event which also called `ProcessCapturedAudio()` via `OnSystemAudioRecordingStopped`. This caused the audio processing pipeline (and potentially the diarization) to run twice.
**What went wrong:** NAudio's `WasapiLoopbackCapture.StopRecording()` fires `RecordingStopped` asynchronously. If the stop handler method also calls the processing method directly, processing runs twice — once from the direct call and once from the event. With diarization, this would mean two concurrent diarization runs on the same audio file, wasting CPU and potentially corrupting state.
**Fix / Lesson:** Only trigger `ProcessCapturedAudio()` from the `RecordingStopped` event handler, never call it directly from `StopRecording()`. **When using event-based APIs like NAudio, let the event be the single source of truth for state transitions. Don't duplicate calls that the event will already trigger.**

### System.Speech produces garbage transcription on WASAPI loopback audio
**Date:** 2026-02-24
**Context:** The v0.2 diarization pipeline sliced audio per speaker segment and fed each slice to System.Speech SpeechRecognitionEngine for transcription. Diarization worked correctly (speaker segments and timestamps were accurate).
**What went wrong:** System.Speech produced completely nonsensical text (e.g., "A seizure was Lamar" from a moon landing video). Root causes: (1) System.Speech was designed for live microphone dictation with trained voice profiles, not pre-recorded system audio; (2) short audio segments (1-5 seconds) provide insufficient context for its language model; (3) its acoustic model is ancient (early 2000s architecture) and never competitive with modern ASR.
**Fix / Lesson:** Replaced System.Speech entirely with sherpa-onnx's OfflineRecognizer using Moonshine Tiny int8 model. Same NuGet package already installed for diarization also provides ASR — no new dependencies needed. Also eliminated audio file slicing: instead of extracting WAV files per segment, load full audio as float[] once and index by timestamp. **System.Speech is not suitable for transcribing non-microphone audio sources. Always use a modern neural ASR model for this use case.**

### LLamaSharp "llama_decode failed: NoKvSlot" on longer transcripts
**Date:** 2026-02-24
**Context:** The LLamaSharp debug window had a default Context Size of 1024 tokens. Short sample transcripts worked fine, but pasting a longer dummy transcript caused inference to fail.
**What went wrong:** The `NoKvSlot` error means the KV (Key-Value) cache is full — the tokenized prompt exceeded the allocated context size. Context size must be large enough to hold both the input tokens AND the output tokens. 1024 was far too small for real meeting transcripts.
**Fix / Lesson:** Increased default context size from 1024 to 4096. Phi-4-mini supports up to 16,384 tokens, so there's room to go higher if needed. Added a user-friendly error message when `NoKvSlot` occurs, telling the user to increase context size. **When choosing context size defaults, consider realistic input sizes, not just minimal test cases.** Note: larger context size = more RAM usage, so don't set it unnecessarily high — 4096 is a good default for most meeting transcripts.

### Missing `return` after early-exit status message caused crash on Stop Recording
**Date:** 2026-02-25
**Context:** `ProcessCapturedAudio()` in NoteTakingWindow checks if ASR models are available before attempting transcription. If models aren't available, it displays an orange status message.
**What went wrong:** The "models not available" code block displayed the error but **did not `return`**. Execution fell through to the `else if`/`else` branches, which called `RunSinglePassTranscriptionAsync()` → `_asrService.TranscribeAsync()` → `GetOrCreateRecognizer()`, which threw `InvalidOperationException` because models weren't downloaded. This unhandled exception crashed the app.
**Fix / Lesson:** Added `return;` after the error status message. **When using if/else-if/else chains where early branches show error messages and should exit, always include an explicit `return` statement. Don't rely on the if/else structure to prevent fall-through when the first branch doesn't have an `else` counterpart — it's an `if` without `else`, so it falls through.**

### sherpa-onnx C# config types are STRUCTS — chained property access silently fails
**Date:** 2026-02-25
**Context:** After implementing multi-model ASR support, the app crashed with a native `AccessViolationException` / segfault when stopping recording. sherpa-onnx stderr showed `tokens: '' does not exist` — all config paths were empty strings despite the code appearing to set them correctly.
**What went wrong:** All sherpa-onnx C# config types (`OfflineRecognizerConfig`, `OfflineModelConfig`, `OfflineWhisperModelConfig`, `OfflineMoonshineModelConfig`, `OfflineSpeakerDiarizationConfig`, `OfflineSpeakerSegmentationModelConfig`, `SpeakerEmbeddingExtractorConfig`, `FastClusteringConfig`) are **structs** with `[StructLayout(LayoutKind.Sequential)]` for P/Invoke marshaling. Writing `config.ModelConfig.Whisper.Encoder = "path"` modifies a **temporary copy** of the nested struct — the original `config` is never changed. This is a classic C# value-type gotcha. The native C++ code receives a config with all-empty-string paths, tries to open files with empty names, fails validation, creates a null recognizer, then segfaults on `CreateStream()`. The crash is a native segfault (`AccessViolationException`) which bypasses all .NET exception handlers — even `AppDomain.UnhandledException` cannot catch it, so the CrashLogService never writes anything.
**Fix / Lesson:** Copy each nested struct level to a local variable, modify the local, then assign it back: `var modelConfig = config.ModelConfig; var whisper = modelConfig.Whisper; whisper.Encoder = "path"; modelConfig.Whisper = whisper; config.ModelConfig = modelConfig;`. Fixed in both `SherpaOnnxASRService.cs` and `SherpaOnnxDiarizationService.cs`. **When using any P/Invoke library with `[StructLayout]` config types, ALWAYS check whether they are structs or classes (use reflection: `typeof(T).IsValueType`). Never chain nested struct property assignments.**
