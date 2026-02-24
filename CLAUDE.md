# Meeting Notes App — AI-Powered Meeting Note-Taker with Notion Integration

## Overview

A Windows desktop app that captures meeting audio, transcribes it in real-time, generates AI summaries, and saves structured notes to Notion. Built with .NET 8 + WPF, using NAudio for system audio capture, Windows Speech Recognition for transcription, and LLamaSharp (in-process local LLM) or a user-provided cloud API key for AI-powered summaries.

**Core Philosophy**: Capture everything, organize automatically, save to where you already work (Notion).

---

## Rules

### Documentation
- Always read ALL `.md` files in `docs/` at the start of each new conversation before writing any code
- UPDATE `docs/folder_filestructure.md` whenever files are added, removed, or moved
- UPDATE `docs/checklist.md` when starting, progressing, or completing work items. Never recreate it — only update existing items or append new ones. Use: `[ ]` not started, `[~]` in progress, `[x]` completed
- UPDATE `docs/lessons.md` whenever a bug, failed approach, or bad pattern is encountered during development. Never delete entries — this is a permanent history so we don't repeat the same mistakes

### Coding Standards
- You are a senior software engineer with expertise in .NET and WPF development
- Follow industry best practices and established design patterns
- Write clean, maintainable, and self-documenting code
- Handle errors gracefully with user-friendly feedback, never crash silently
- Keep the codebase simple and focused — avoid over-engineering or premature abstraction
- Use async/await properly to keep the UI thread responsive
- Follow Microsoft's .NET naming conventions and C# coding standards
- **NO FALLBACK LOGIC, NO LEGACY CODE, NO DEPRECATED CODE.** If code, a feature, or an approach is no longer the current plan, REMOVE IT COMPLETELY. Do not keep old implementations "just in case" or wrap them behind flags. Do not add fallback paths that silently degrade to a worse experience. If the current approach fails, show a clear error message — do not silently fall back to a different approach. When plans change (e.g., LMStudio → LLamaSharp), DELETE the old code entirely and replace it with the new approach. This applies to all code, services, UI elements, and documentation references.
- **ALWAYS CHECK TO SEE IF CODE/METHOD/CLASS/HELPERS/etc. ALREADY EXISTS BEFORE WRITING IT. USE @/docs/folder_filestructure.md TO FIND THE FILES AND FOLDERS.**
- Be careful with audio/speech resources — always dispose NAudio captures, speech engines, and streams properly
- Notion API calls should always handle rate limits, auth failures, and network errors gracefully

### Product
- **Privacy-first**: All transcription happens locally. AI summarization defaults to local (Private Mode via LLamaSharp). Optional cloud API mode (API Key Mode) requires explicit user opt-in with their own API key.
- **Notion is the source of truth**: Notes are saved to the user's own Notion workspace — we don't store meeting data locally beyond the session
- **Two AI modes**: Private Mode (LLamaSharp, local, default) and API Key Mode (cloud provider, opt-in). No other AI backends.

---

## Documentation

READ **ALL** of these docs at the start of every conversation, and update them as needed:

| Doc | Purpose |
|-----|---------|
| `docs/goals.md` | Product goals, scope, non-goals, success metrics |
| `docs/techstack.md` | Tech stack decisions and technical architecture |
| `docs/features.md` | Feature specifications (all windows and interactions) |
| `docs/datamodels.md` | Data models, classes, and storage schema |
| `docs/ui.md` | UI design, layout, color system, and user flows |
| `docs/niche.md` | Target niche and pain points |
| `docs/marketing.md` | Messaging, positioning, and feature highlights |
| `docs/roadmap.md` | Version roadmap (current state through future) |
| `docs/checklist.md` | Development progress tracker (living document) |
| `docs/folder_filestructure.md` | Codebase file structure (update on any file changes) |
| `docs/lessons.md` | Lessons learned — bugs, failed approaches, patterns to avoid |
| `docs/dev-workflow.md` | Build instructions, prerequisites, and development workflow |
| `docs/README.md` | Project readme |

---

## Edge Cases & Decisions

| Scenario | Decision |
|----------|----------|
| No Notion API key configured | Disable database selection, show setup prompt in Settings |
| Private Mode: model not downloaded | Show clear message in AI Summary area: "AI model not downloaded. Go to Settings → AI Settings to download." Disable Generate Summary button. |
| Private Mode: inference fails | Show error with details in AI Summary area. Do not fall back to text truncation or any other approach. |
| API Key Mode: API call fails | Show error with HTTP status and provider details. Do not fall back to local inference or text truncation. |
| No system audio detected | Show clear status message, suggest checking audio output device |
| Speech recognition produces no results | Display "No speech detected" status, don't append empty text |
| Notion API rate limit hit | Retry with backoff, show progress to user |
| Meeting note save fails | Show error with details, keep note window open so user doesn't lose work |
| No databases found with "Meetings" filter | Show all databases, or prompt user to create one |
| Long meeting transcription exceeds Notion rich_text limit (2000 chars) | Split into multiple blocks or truncate with warning |

---

## AGENT INSTRUCTIONS / USAGE INSTRUCTIONS

To produce higher-quality, more reliable code, leverage subagents in Claude Code for specialized research and tasks. Key subagent strategies for this project:

- Spin off a subagent for **audio capture and transcription research** to investigate NAudio WASAPI loopback capture, System.Speech recognition accuracy improvements, and potential Whisper integration paths. Have it research audio format conversion, sample rate requirements, and real-time vs batch processing trade-offs.
- Spin off a subagent for **Notion API research** to deeply investigate database schema requirements, property types (title, rich_text, date, etc.), pagination for large databases, rate limiting, and block-level content creation. Cross-reference with the official Notion API docs.
- Spin off a subagent for **LLamaSharp/local LLM integration** to research optimal prompt engineering for meeting summaries, model selection, structured output formats, and handling long transcriptions that exceed context windows. Reference `docs/llamasharp/` for LLamaSharp documentation.
- Spin off a subagent for **WPF UI patterns** to research modern WPF styling, data binding best practices, and MVVM migration strategies for the current code-behind architecture.

**ALWAYS USE THE `/agents` COMMAND TO CREATE AND CHAIN SUBAGENTS.**
**PLEASE GIVE THE AGENT A CLEAR AND CONCISE NAME OF THE TASK AT HAND.**
