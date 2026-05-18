# Design: Work Tracker

**Status:** Complete  
**Follows:** spec.md  
**Scope:** Large — WinForms tray app, SQLite, background scheduler

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                      Program.cs                         │
│          Application.Run(TrayApplicationContext)         │
└──────────────────────┬──────────────────────────────────┘
                       │ owns
          ┌────────────▼────────────┐
          │  TrayApplicationContext │  ← NotifyIcon + ContextMenu
          │  (ApplicationContext)   │     manages form instances
          └──┬──────────┬───────────┘
             │          │ creates
    starts   │     ┌────▼──────────────┐
             │     │  PromptScheduler  │  ← background Timer thread
             │     │                  │     fires daily prompts
             │     └────┬─────────────┘
             │          │ checks before firing
             │     ┌────▼──────────────┐
             │     │ScreenShareDetector│  ← P/Invoke EnumWindows
             │     └───────────────────┘
             │
    ┌────────▼──────────────────────────────────────────┐
    │                    Forms                          │
    │  PromptForm  StartWorkForm  FinishWorkForm        │
    │  AddEntryForm  WeekViewForm  SettingsForm         │
    └────────┬──────────────────────────────────────────┘
             │ calls
    ┌────────▼──────────────────────────────────────────┐
    │                  Services                         │
    │  WindowTitleScanner  CsvExportService             │
    │  AutoStartService                                 │
    └────────┬──────────────────────────────────────────┘
             │ calls
    ┌────────▼──────────────────────────────────────────┐
    │                  Data Layer                       │
    │  WorkEntryRepository   SettingsRepository         │
    │  DatabaseInitializer                              │
    └────────┬──────────────────────────────────────────┘
             │
    ┌────────▼──────────┐
    │   SQLite (local)   │
    │   worktracker.db   │
    └────────────────────┘
```

---

## Solution Structure

```
WorkTracker/
├── WorkTracker.csproj          (.NET 8, WinForms, x64)
├── Program.cs
├── App/
│   ├── TrayApplicationContext.cs
│   └── PromptScheduler.cs
├── Forms/
│   ├── PromptForm.cs / .Designer.cs
│   ├── StartWorkForm.cs / .Designer.cs
│   ├── FinishWorkForm.cs / .Designer.cs
│   ├── AddEntryForm.cs / .Designer.cs
│   ├── WeekViewForm.cs / .Designer.cs
│   └── SettingsForm.cs / .Designer.cs
├── Services/
│   ├── ScreenShareDetector.cs
│   ├── WindowTitleScanner.cs
│   ├── CsvExportService.cs
│   └── AutoStartService.cs
├── Data/
│   ├── DatabaseInitializer.cs
│   ├── WorkEntryRepository.cs
│   └── SettingsRepository.cs
├── Models/
│   ├── WorkEntry.cs
│   └── AppSettings.cs
└── Resources/
    └── tray.ico
```

### NuGet packages
| Package | Purpose |
|---------|---------|
| `Microsoft.Data.Sqlite` | SQLite access |
| `CsvHelper` | CSV export |

---

## Database Schema

```sql
CREATE TABLE IF NOT EXISTS work_entries (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    date        TEXT NOT NULL,      -- "yyyy-MM-dd"
    start_time  TEXT NOT NULL,      -- "HH:mm"
    end_time    TEXT,               -- "HH:mm" — NULL while session is open
    total_hours REAL,               -- NULL while session is open
    description TEXT,
    created_at  TEXT NOT NULL       -- "yyyy-MM-dd HH:mm:ss"
);

CREATE TABLE IF NOT EXISTS app_settings (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

-- Default settings rows (seeded on first run):
-- prompt_window1_start  = "10:00"
-- prompt_window1_end    = "11:00"
-- prompt_window2_start  = "15:00"
-- prompt_window2_end    = "16:00"
-- artia_activity_id     = ""
-- user_email            = "paolo.fernandes@lyncas.net"
```

**Pending session:** A `work_entries` row where `end_time IS NULL`. Only one such row should exist at a time. Enforced in `WorkEntryRepository`.

---

## Component Designs

### Program.cs
```
- Set ApplicationContext to TrayApplicationContext
- Enable visual styles + compatible text rendering
- Application.Run(new TrayApplicationContext())
```
No `Application.SetCompatibleTextRenderingDefault` needed in .NET 8 — set it once.  
No `MainForm` — the app lives entirely in the tray.

---

### TrayApplicationContext
```
Responsibilities:
- Create and configure NotifyIcon + ContextMenuStrip
- Wire menu items to handler methods
- Hold singleton reference to WeekViewForm (create on first Open, reuse after)
- Pass PromptScheduler events → PromptForm
- Handle Exit (dispose icon, close scheduler, Application.Exit)

Menu item handlers:
- Open          → show/bring-to-front WeekViewForm
- Start Work    → ShowDialog(new StartWorkForm())
- Add Entry     → ShowDialog(new AddEntryForm())
- View Week     → same as Open
- Export        → ShowDialog(new ExportRangeForm()) → CsvExportService
- Finish Work   → ShowDialog(new FinishWorkForm())
- Settings      → ShowDialog(new SettingsForm())
- Exit          → cleanup + Application.Exit()

State guard (Finish Work):
- If no open session exists → show MessageBox("No active work session.")
- If open session exists → show FinishWorkForm
```

---

### PromptScheduler
```
Responsibilities:
- On start: calculate next fire time for Window 1 and Window 2 (random within range)
- Run a System.Threading.Timer that ticks every 30 seconds
- On each tick:
    1. Check if current time has passed a scheduled fire time
    2. If yes AND it's a weekday (DayOfWeek != Saturday/Sunday):
        a. Check ScreenShareDetector.IsSharing()
        b. If sharing → skip this tick, retry on next tick (30s)
        c. If not sharing → raise PromptDue event, reschedule that window for tomorrow
    3. Handle delayed prompts: if a delay was requested, store DelayUntil timestamp
       and only fire when DateTime.Now >= DelayUntil

Fire time calculation:
- Random r = new Random()
- fireTime = windowStart + TimeSpan.FromMinutes(r.Next(0, (int)(windowEnd - windowStart).TotalMinutes))
- Scheduled for today if not yet passed; otherwise tomorrow

Delay logic:
- On delay request (15/30/60 min): set DelayUntil = DateTime.Now + delay
- Do NOT reschedule the window end check — fire anyway after delay
- On next scheduler tick after DelayUntil: fire if not sharing

Events:
- event EventHandler PromptDue  ← TrayApplicationContext subscribes, shows PromptForm
```

---

### ScreenShareDetector
```
Strategy: check for known screen-sharing app indicators via P/Invoke

Zoom: FindWindow(null, null) scan for class "ZPToolbar" 
      (Zoom renders a toolbar window during active screen share)

Teams: EnumWindows scan for process "ms-teams" or "Teams" with window title
       containing "sharing" or "presenting"

Google Meet: browser-based — not detectable in v1 (documented limitation)

Implementation:
- [DllImport("user32.dll")] FindWindow
- [DllImport("user32.dll")] EnumWindows + GetWindowText + GetWindowThreadProcessId
- bool IsSharing() → returns true if any sharing indicator found

Fallback: if detection fails (exception) → treat as not sharing (don't block prompt)
```

---

### WindowTitleScanner
```
Responsibilities:
- Enumerate all visible window titles via EnumWindows P/Invoke
- Apply regex patterns to extract ticket numbers
- Return the best match (from most-recently-focused window)

Patterns:
- Azure DevOps: Regex(@"(?:AB)?#(\d+)")          → captures ticket number
- Jira:         Regex(@"\b([A-Z]{2,10}-\d+)\b")  → captures ticket key

Foreground priority:
- Call GetForegroundWindow() first
- GetWindowText on that handle → check patterns
- If found → return immediately
- If not found → EnumWindows all visible windows, return first match

Returns: string? (null if nothing found)
Format returned: raw ticket string, e.g. "1234" or "PROJ-123"
Caller formats: "Are you still working on task #1234?"
```

---

### Forms

#### PromptForm (automated daily prompt)
```
Layout: small dialog (~420×220px), no resize, topmost
Controls:
- Label: "What are you working on?"
- DateTimePicker (time only): "Start time" — defaults to current time
- TextBox: "Description" — pre-filled by WindowTitleScanner if match found
- Button: "Confirm" → saves pending entry (start time only, no end)
- ComboBox + Button: "Delay" — values: 15 min, 30 min, 60 min
  → on click, notifies PromptScheduler of delay duration
FormClosing: treat as Delay 15 min (same as clicking Delay without picking)
```

#### StartWorkForm
```
Layout: small dialog (~360×160px)
Controls:
- Label: "What time did you start working?"
- DateTimePicker (time only) — defaults to current time
- Button: "Start" → calls WorkEntryRepository.CreatePendingEntry(startTime)
Guard: check for existing open session first; if found show confirmation to close it
```

#### FinishWorkForm
```
Layout: small dialog (~420×220px)
Controls:
- Label: "What time did you finish?"
- DateTimePicker (time only) — defaults to current time
- TextBox: "Description" — pre-filled by WindowTitleScanner
- Label (computed): "Total: X hours" — updates live as end time changes
- Button: "Save" → closes pending session, calculates total_hours, persists
```

#### AddEntryForm
```
Layout: dialog (~420×280px)
Controls:
- DateTimePicker (date only) — defaults to today
- DateTimePicker (time): Start time
- DateTimePicker (time): End time
- TextBox: Description
- Label (computed): "Total: X hours" — live calculation
- Button: "Save" → WorkEntryRepository.AddEntry(...)
```

#### WeekViewForm
```
Layout: resizable window (~800×500px), singleton (one instance, reused)
Controls:
- Top bar:
  - "◀ Prev Week" button
  - Label showing current week range ("12 May – 18 May 2025")
  - "Next Week ▶" button
  - DateTimePicker (from) + DateTimePicker (to) for custom range
  - "Apply" button for custom range
- DataGridView (fills remaining space):
  Columns: Date | Start | End | Hours | Description | [Delete]
  - Rows grouped by date (date shown on first row of each group, empty below)
  - Day subtotal row (bold, no Start/End, Hours = sum for day)
  - Week total row at bottom (bold)
  - Double-click cell → inline edit (DataGridView native edit mode)
  - Delete column: button per row → confirm dialog → delete
- Bottom bar:
  - "Export" button → opens save dialog for current displayed range
  - "Add Entry" button → opens AddEntryForm
```

#### SettingsForm
```
Layout: dialog (~400×320px)
Controls:
- Section "Prompt Windows":
  - Prompt 1: DateTimePicker(start) → DateTimePicker(end)
  - Prompt 2: DateTimePicker(start) → DateTimePicker(end)
- Section "Artia Export":
  - TextBox: ID da Atividade (monthly, user updates each month)
  - TextBox: E-mail
- Button: "Save" → SettingsRepository.Save() + restart scheduler with new windows
- Button: "Cancel"
```

---

### Data Layer

#### DatabaseInitializer
```
- Runs on startup before anything else
- db path: %APPDATA%\WorkTracker\worktracker.db
- Creates directory if not exists
- Runs CREATE TABLE IF NOT EXISTS for both tables
- Seeds default settings rows if app_settings is empty
```

#### WorkEntryRepository
```
Methods:
- CreatePendingEntry(date, startTime) → INSERT with null end_time
- CloseEntry(id, endTime, description) → UPDATE set end_time, total_hours, description
- GetOpenSession() → SELECT WHERE end_time IS NULL (should be 0 or 1 row)
- AddEntry(WorkEntry) → INSERT complete entry
- GetEntriesByRange(from, to) → SELECT WHERE date BETWEEN
- UpdateEntry(WorkEntry) → UPDATE by id
- DeleteEntry(id) → DELETE by id
```

#### SettingsRepository
```
Methods:
- Get(key) → SELECT value WHERE key
- Set(key, value) → INSERT OR REPLACE
- GetAll() → returns AppSettings model (maps all keys)
- SaveAll(AppSettings) → batch upsert
```

#### Models

```csharp
class WorkEntry {
    int Id;
    DateOnly Date;
    TimeOnly StartTime;
    TimeOnly? EndTime;
    double? TotalHours;
    string? Description;
    DateTime CreatedAt;
}

class AppSettings {
    TimeOnly Prompt1Start;  // default 10:00
    TimeOnly Prompt1End;    // default 11:00
    TimeOnly Prompt2Start;  // default 15:00
    TimeOnly Prompt2End;    // default 16:00
    string ArtiaActivityId;
    string UserEmail;
}
```

---

### CsvExportService
```
Input: IEnumerable<WorkEntry>, AppSettings
Output: CSV file at user-chosen path

Column mapping:
- Data            → entry.Date.ToString("dd/MM/yyyy")
- Hora de início  → entry.StartTime.ToString("HH:mm")
- Esforço         → entry.TotalHours.ToString("0.##")
- Observação      → entry.Description
- ID da Atividade → settings.ArtiaActivityId
- E-mail          → settings.UserEmail

Uses CsvHelper with custom ClassMap to set Portuguese headers.
```

### AutoStartService
```
Registry path: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
Key name: "WorkTracker"
Value: full path to WorkTracker.exe

Methods:
- Enable() → set registry key
- Disable() → delete registry key
- IsEnabled() → check if key exists and value matches current exe path

Called during DatabaseInitializer (first run) → Enable() automatically.
```

---

## Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Tray pattern | `ApplicationContext` subclass, no MainForm | Standard WinForms tray pattern; clean lifecycle |
| DB access | `Microsoft.Data.Sqlite` raw SQL | No ORM overhead for a simple schema; easier to audit |
| Scheduler | `System.Threading.Timer` + 30s tick | Simple, reliable, no external dependency |
| Screen share detection | P/Invoke `FindWindow` / `EnumWindows` | Only practical Windows-native approach without heavy SDK |
| WeekViewForm | Singleton in TrayApplicationContext | Avoid duplicate windows; bring-to-front if already open |
| Settings storage | SQLite `app_settings` table (key-value) | One file for everything; easy to add new settings later |
| CSV library | `CsvHelper` | Handles encoding, quoting, and custom headers cleanly |

---

## OQ-2 Resolution: Screen Sharing Detection

**Approach:** Process-window scanning via `EnumWindows` P/Invoke.

- **Zoom:** Detect `ZPToolbar` window class (present only during active share)
- **Microsoft Teams:** Scan for `ms-teams.exe` process windows with "sharing" in window text
- **Limitation (documented):** Google Meet (browser-based) and Webex are not detected in v1. If screen is shared via those tools, prompt appears normally.

---

## Data Flow: Daily Prompt

```
[Timer tick] → is weekday? → is DelayUntil passed (or none set)?
    → IsSharing()? → if yes: wait next tick
    → if no: raise PromptDue
        → TrayApplicationContext.OnPromptDue()
            → WindowTitleScanner.Scan() → ticket string?
            → new PromptForm(ticket).ShowDialog()
                → Confirm: WorkEntryRepository.CreatePendingEntry(startTime, desc)
                → Delay: PromptScheduler.Delay(minutes)
                → Close: PromptScheduler.Delay(15)
```

---

## Open Questions Resolved

| OQ | Resolution |
|----|------------|
| OQ-2 | Screen sharing: P/Invoke Zoom `ZPToolbar` + Teams window scan |
| OQ-3 | App name: "Work Tracker" (can change before packaging) |
| OQ-4 | Tray: WinForms `NotifyIcon` + `ContextMenuStrip` |
