# ui.md

## UI Principles
- **Dark theme** — comfortable for extended meeting use
- **Muted grey-green accent** — professional, non-distracting
- **Status-first** — always show recording/connection state clearly
- **Minimal navigation** — Main Window → Note-Taking or Settings, that's it
- **Non-blocking** — note-taking window opens independently, main window stays accessible

---

## Color System (`Styles/Colors.xaml`)

### Primary Colors (Muted Grey-Green)
| Key | Color | Usage |
|-----|-------|-------|
| PrimaryBrush | `#5A7B6A` | Header backgrounds, primary buttons |
| PrimaryDarkBrush | `#4A6B5A` | Darker variant for hover/secondary actions |
| AccentBrush | `#7B9A8A` | Accent text, secondary highlights |

### Background Colors (Dark Theme)
| Key | Color | Usage |
|-----|-------|-------|
| LightBrush | `#1A1A1A` | Window backgrounds |
| DarkBrush | `#0F0F0F` | Deepest background |
| SurfaceBrush | `#2A2A2A` | Card/section backgrounds |
| SurfaceElevatedBrush | `#3A3A3A` | Elevated cards |

### Text Colors
| Key | Color | Usage |
|-----|-------|-------|
| TextPrimaryBrush | `#FFFFFF` | Primary text |
| TextSecondaryBrush | `#E0E0E0` | Secondary/helper text |

### Status Colors
| Key | Color | Usage |
|-----|-------|-------|
| SuccessBrush | `#4A6B5A` | Connected, monitoring, recording complete |
| WarningBrush | `#8B6B3A` | Disabled, processing, listening |
| ErrorBrush | `#8B4A4A` | Errors, stopped recording |
| InfoBrush | `#4A6B8B` | Recording active, informational |

---

## Window Layouts

### Main Window (1000x800)
```
+--- Meeting Notes ----------------------------[⚙]--+
| [●] Ready — Waiting for calls to be detected      |
|                                                     |
| ┌─ Save Note ────────────────────────────────────┐ |
| │ Select database: [Dropdown ▼]                   │ |
| │ [Start Notes]                                   │ |
| │ Configure workspace integrations in Settings    │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Test Functions ───────────────────────────────┐ |
| │ [Simulate Call] [Test Notion] [Start Meeting]  │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Recent Notes ─────────────────────────────────┐ |
| │ Meeting Title                                   │ |
| │ Oct 1, 2024 at 2:30 PM                        │ |
| │ First 100 chars of notes...                    │ |
| └─────────────────────────────────────────────────┘ |
+-----------------------------------------------------+
```

### Note-Taking Window (900x700)
```
+--- Recording Meeting Notes --- Meeting Title ------+
|                                                     |
| ┌─ Meeting Information ──────────────────────────┐ |
| │ Title:     [editable]     Database: DB Name    │ |
| │ Organizer: [editable]     Started:  10:30 AM   │ |
| │ Attendees: [editable]     Duration: 00:05:30   │ |
| │                           Status: ● Recording   │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Live Transcription ── ● Recording [Stop Rec] ─┐ |
| │ Transcribed text appears here as audio is       │ |
| │ processed...                                    │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Your Notes ───────────────────────────────────┐ |
| │ [Multi-line text editor]                        │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ AI Summary ───────────────────────────────────┐ |
| │ [Read-only summary area]                        │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Key Points ───────────────────────────────────┐ |
| │ ☐ Point 1    ☑ Point 2    ☐ Point 3           │ |
| │ [New point input] [Add]                        │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Action Items ─────────────────────────────────┐ |
| │ ☐ Task 1 (John)  ☐ Task 2 (Sarah)  ☑ Task 3  │ |
| │ [New item input] [Add]                         │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
|     [Stop Recording]  [Save to Notion]  [Summary]   |
+-----------------------------------------------------+
```

### Settings Window (900x700)
```
+--- Settings --- Manage integrations and prefs -----+
|                                                     |
| ┌─ Notion Workspace Integrations ─── [Add] ─────┐ |
| │ ┌─ Workspace Name ──────── [Edit][Test][Del] ┐ │ |
| │ │ workspace-abc123                            │ │ |
| │ │ Selected Database Name                      │ │ |
| │ │ ● Connected                                 │ │ |
| │ └────────────────────────────────────────────┘ │ |
| │                                                 │ |
| │ ┌─ Add/Edit Form (collapsible) ──────────────┐ │ |
| │ │ Name: [______]                              │ │ |
| │ │ API Key: [********] [Fetch Databases]       │ │ |
| │ │ Database: [Dropdown ▼]                      │ │ |
| │ │ [Save] [Cancel]                             │ │ |
| │ └────────────────────────────────────────────┘ │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ AI Settings ──────────────────────────────────┐ |
| │ LMStudio: http://127.0.0.1:1234               │ |
| │ [Test LMStudio Connection]                     │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Call Detection Settings ──────────────────────┐ |
| │ ☑ Enable automatic call detection             │ |
| │ ☑ Microsoft Teams — Monitoring                 │ |
| │ ☑ Zoom — Monitoring                           │ |
| │ ☑ Google Meet — Monitoring                    │ |
| │ ☐ Discord — Disabled                          │ |
| └─────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ General Settings ────────────────────────────┐  |
| │ ☐ Start minimized to system tray              │  |
| │ ☑ Auto-save notes during meetings             │  |
| │ ☑ Show desktop notifications                  │  |
| └────────────────────────────────────────────────┘  |
|                                                     |
|                                         [Close]     |
+-----------------------------------------------------+
```

---

## User Flows

### Start a Meeting Note
1. User opens app → Main Window
2. Selects a Notion database from dropdown
3. Clicks "Start Notes"
4. NoteTakingWindow opens with selected workspace/database
5. User clicks "Start Recording" to capture audio
6. User takes manual notes alongside transcription
7. User clicks "Stop Recording" when done
8. User clicks "Generate Summary" for AI overview
9. User clicks "Save to Notion" to persist everything

### Configure a Workspace (First Run)
1. User clicks gear icon → Settings Window
2. Clicks "Add Workspace"
3. Enters display name
4. Pastes Notion API key
5. Clicks "Fetch Databases" → dropdown populates with matching databases
6. Selects target database
7. Clicks "Save"
8. Closes Settings → database appears in Main Window dropdown

---

## Shared Styles (`Styles/Styles.xaml`)

Common button, text, and container styles defined in `Styles/Styles.xaml` and applied globally via `App.xaml` merged dictionaries. All windows inherit the dark theme automatically.

## Design Language
- **Corner radius**: 10px for cards/sections, 5px for list items
- **Padding**: 15-20px for sections, 8-12px for buttons
- **Font sizes**: 24px headers, 18px section titles, 14-16px body, 12px helper text
- **Status indicators**: 8-12px colored ellipses with accompanying text
- **Buttons**: Primary actions use PrimaryBrush background, destructive use ErrorBrush, neutral use Gray600Brush
