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

### LLamaSharp "llama_decode failed: NoKvSlot" on longer transcripts
**Date:** 2026-02-24
**Context:** The LLamaSharp debug window had a default Context Size of 1024 tokens. Short sample transcripts worked fine, but pasting a longer dummy transcript caused inference to fail.
**What went wrong:** The `NoKvSlot` error means the KV (Key-Value) cache is full — the tokenized prompt exceeded the allocated context size. Context size must be large enough to hold both the input tokens AND the output tokens. 1024 was far too small for real meeting transcripts.
**Fix / Lesson:** Increased default context size from 1024 to 4096. Phi-4-mini supports up to 16,384 tokens, so there's room to go higher if needed. Added a user-friendly error message when `NoKvSlot` occurs, telling the user to increase context size. **When choosing context size defaults, consider realistic input sizes, not just minimal test cases.** Note: larger context size = more RAM usage, so don't set it unnecessarily high — 4096 is a good default for most meeting transcripts.
