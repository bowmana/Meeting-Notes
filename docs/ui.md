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
| PrimaryBrush | `#5A7B6A` | Header backgrounds, primary buttons, active sidebar item accent |
| PrimaryDarkBrush | `#4A6B5A` | Darker variant for hover/secondary actions |
| AccentBrush | `#7B9A8A` | Accent text, secondary highlights |

### Background Colors (Dark Theme)
| Key | Color | Usage |
|-----|-------|-------|
| LightBrush | `#1A1A1A` | Window backgrounds |
| DarkBrush | `#0F0F0F` | Deepest background |
| SurfaceBrush | `#2A2A2A` | Card/section backgrounds, sidebar background |
| SurfaceElevatedBrush | `#3A3A3A` | Elevated cards, provider picker cards |

### Text Colors
| Key | Color | Usage |
|-----|-------|-------|
| TextPrimaryBrush | `#FFFFFF` | Primary text |
| TextSecondaryBrush | `#E0E0E0` | Secondary/helper text, category headers |

### Status Colors
| Key | Color | Usage |
|-----|-------|-------|
| SuccessBrush | `#4A6B5A` | Connected, monitoring, recording complete |
| WarningBrush | `#8B6B3A` | "Coming soon" badges, processing, listening |
| ErrorBrush | `#8B4A4A` | Errors, stopped recording |
| InfoBrush | `#4A6B8B` | Recording active, informational |

### Integration Provider Badge Colors
| Provider | Badge Color | Badge Text |
|----------|-------------|------------|
| Notion | `#000000` (white text) | N |
| Google Drive | `#4285F4` (white text) | G |
| OneDrive/SharePoint | `#0078D4` (white text) | O |
| Confluence | `#0052CC` (white text) | C |
| Slack | `#4A154B` (white text) | S |
| CSV | `Gray600Brush` (white text) | CSV |
| Excel | `#217346` (white text) | XLS |
| Markdown | `Gray600Brush` (white text) | MD |
| PDF | `#FF0000` (white text) | PDF |
| Webhook | `Gray600Brush` (white text) | {} |

---

## Window Layouts

### Main Window (1000x800)
```
+--- Meeting Notes ----------------------------[⚙]--+
| [●] Ready — Waiting for calls to be detected      |
|                                                     |
| ┌─ Start a Meeting ─────────────────────────────────┐ |
| │ ● Connected to Notion · Work Notion               │ |
| │ Save notes to: [[N] Work Notion — Sprint DB ▼]   │ |
| │ [Start Meeting]                                    │ |
| │                                                    │ |
| │ (or if no integrations configured:)                │ |
| │ Get Started                                        │ |
| │ Choose where to save your meeting notes —          │ |
| │ Notion, CSV, Excel, and more.                      │ |
| │ [Add Integration]                                  │ |
| └────────────────────────────────────────────────────┘ |
|                                                     |
| ┌─ Test Functions ───────────────────────────────┐ |
| │ [Simulate Call] [Test Connection] [Start]      │ |
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
|     [Stop Recording]  [Save to Notion*]  [Summary]  |
|     * Button text is dynamic per integration:        |
|       Notion → "Save to Notion"                      |
|       CSV → "Export to CSV"                          |
|       Excel → "Export to Excel"  etc.                |
+-----------------------------------------------------+
```

### Settings Window (900x700) — Sidebar Navigation
```
+--- Settings ─────────────────────────────────────────+
|                                                       |
| ┌────────────┐  ┌─────────────────────────────────┐  |
| │            │  │                                   │  |
| │ ▌Integra-  │  │  Integrations    [+ Add Integration]│|
| │  tions     │  │  Manage where your notes are saved│  |
| │            │  │                                   │  |
| │  Speech &  │  │  ┌─────────────────────────────┐ │  |
| │  Audio     │  │  │ [N] Work Notion    Connected│ │  |
| │            │  │  │     Notion · Sprint DB       │ │  |
| │  AI        │  │  │              [Edit] [Delete] │ │  |
| │            │  │  └─────────────────────────────┘ │  |
| │  General   │  │                                   │  |
| │            │  │  ┌─────────────────────────────┐ │  |
| │            │  │  │ [N] Personal     Connected  │ │  |
| │            │  │  │     Notion · Meeting Archive │ │  |
| │            │  │  │              [Edit] [Delete] │ │  |
| │            │  │  └─────────────────────────────┘ │  |
| │            │  │                                   │  |
| └────────────┘  └─────────────────────────────────┘  |
|                                                       |
|                                            [Close]    |
+───────────────────────────────────────────────────────+
```

### Provider Picker (Overlay in Integrations Page)
```
+── Add Integration ───────────────────────────────────+
|                                                       |
|  Where do you want to save your meeting notes?        |
|                                                       |
|  CLOUD SERVICES                                       |
|  ┌─────────┐ ┌─────────┐ ┌─────────┐                |
|  │ [N]     │ │ [G]     │ │ [O]     │                |
|  │ Notion  │ │ Google  │ │ OneDrive│                |
|  │ Save to │ │ Drive   │ │ Save to │                |
|  │ Notion  │ │ Save as │ │ OneDrive│                |
|  │ DB      │ │ docs    │ │ or SPO  │                |
|  │         │ │ Soon    │ │ Soon    │                |
|  └─────────┘ └─────────┘ └─────────┘                |
|  ┌─────────┐ ┌─────────┐                            |
|  │ [C]     │ │ [S]     │                            |
|  │ Conflu- │ │ Slack   │                            |
|  │ ence    │ │ Post    │                            |
|  │ Save to │ │ summ-   │                            |
|  │ wiki    │ │ aries   │                            |
|  │ Soon    │ │ Soon    │                            |
|  └─────────┘ └─────────┘                            |
|                                                       |
|  LOCAL EXPORTS                                        |
|  ┌─────────┐ ┌─────────┐ ┌─────────┐                |
|  │ [CSV]   │ │ [XLS]   │ │ [MD]    │                |
|  │ CSV     │ │ Excel   │ │ Markdown│                |
|  │ Export  │ │ Export  │ │ Export  │                |
|  │ as .csv │ │ as .xlsx│ │ as .md  │                |
|  │ Soon    │ │ Soon    │ │ Soon    │                |
|  └─────────┘ └─────────┘ └─────────┘                |
|  ┌─────────┐ ┌─────────┐                            |
|  │ [PDF]   │ │ [{}]    │                            |
|  │ PDF     │ │ Webhook │                            |
|  │ Export  │ │ Custom  │                            |
|  │ as .pdf │ │ API     │                            |
|  │ Soon    │ │ Soon    │                            |
|  └─────────┘ └─────────┘                            |
|                                            [Cancel]   |
+───────────────────────────────────────────────────────+
```

---

## User Flows

### Start a Meeting Note
1. User opens app → Main Window
2. If no integrations configured: clicks "Add Integration" → Settings opens to Integrations page
3. Selects an integration from the integration selector dropdown
4. Clicks "Start Meeting"
5. NoteTakingWindow opens with selected integration
6. User clicks "Start Recording" to capture audio
7. User takes manual notes alongside transcription
8. User clicks "Stop Recording" when done
9. User clicks "Generate Summary" for AI overview
10. User clicks dynamic save button (e.g., "Save to Notion") to persist everything

### Add an Integration (First Run)
1. User clicks gear icon → Settings Window → Integrations page
2. Clicks "+ Add Integration"
3. Provider picker grid appears with Cloud Services and Local Exports categories
4. User clicks a provider card (e.g., Notion)
5. Provider-specific configuration form appears with "< Back" link
6. User fills in provider-specific fields (e.g., display name, API key, target database)
7. Clicks "Save Integration"
8. Integration appears in the integration list
9. Closes Settings → integration appears in Main Window dropdown

### Add a Notion Integration
1. Clicks Notion card in provider picker
2. Reviews Quick Setup Guide (3-step Notion API setup)
3. Enters display name (e.g., "Work Notion")
4. Pastes Notion API key
5. Clicks "Fetch Databases" → dropdown populates with matching databases
6. Selects target database
7. Clicks "Save Integration"

### Add a CSV Export (Future)
1. Clicks CSV card in provider picker
2. Enters display name (e.g., "Weekly Export")
3. Selects export folder via Browse dialog
4. Clicks "Save Integration"

---

## Shared Styles (`Styles/Styles.xaml`)

Common button, text, and container styles defined in `Styles/Styles.xaml` and applied globally via `App.xaml` merged dictionaries. All windows inherit the dark theme automatically.

## Design Language
- **Corner radius**: 10px for cards/sections, 5px for list items
- **Padding**: 15-20px for sections, 8-12px for buttons
- **Font sizes**: 24px headers, 18px section titles, 14-16px body, 12px helper text
- **Status indicators**: 8-12px colored ellipses with accompanying text
- **Buttons**: Primary actions use PrimaryBrush background, destructive use ErrorBrush, neutral use Gray600Brush
