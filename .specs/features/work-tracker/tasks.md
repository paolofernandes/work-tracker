# Tasks: Work Tracker

**Status:** Ready for execution  
**Follows:** design.md  
**Legend:** [P] = can run in parallel with siblings at same level

---

## TASK-01 — Project scaffold
**What:** Create the .NET 8 WinForms solution and project with all folders and NuGet packages  
**Where:** `C:\Dev\Repos\WorkTracker\`  
**Depends on:** nothing  
**Steps:**
1. `dotnet new winforms -n WorkTracker -f net8.0-windows`
2. Add NuGet: `Microsoft.Data.Sqlite`, `CsvHelper`
3. Create folder structure: `App/`, `Forms/`, `Services/`, `Data/`, `Models/`, `Resources/`
4. Set project to x64, single-file publish ready
5. Add `app.manifest` to request `asInvoker` execution level (no UAC prompt)
**Done when:** `dotnet build` succeeds with zero errors  
**Gate:** `dotnet build WorkTracker.csproj`

---

## TASK-02 — Models [P]
**What:** Define `WorkEntry` and `AppSettings` model classes  
**Where:** `Models/WorkEntry.cs`, `Models/AppSettings.cs`  
**Depends on:** TASK-01  
**Steps:**
1. `WorkEntry`: Id (int), Date (DateOnly), StartTime (TimeOnly), EndTime (TimeOnly?), TotalHours (double?), Description (string?), CreatedAt (DateTime)
2. `AppSettings`: Prompt1Start, Prompt1End, Prompt2Start, Prompt2End (TimeOnly), ArtiaActivityId (string), UserEmail (string)
3. Add static `AppSettings.Defaults` returning the default values from spec  
**Done when:** both classes compile, no warnings  
**Gate:** `dotnet build`

---

## TASK-03 — Database initializer [P]
**What:** `DatabaseInitializer` creates the SQLite DB file and runs schema migrations  
**Where:** `Data/DatabaseInitializer.cs`  
**Depends on:** TASK-01  
**Steps:**
1. Resolve DB path: `%APPDATA%\WorkTracker\worktracker.db` — create directory if missing
2. Run `CREATE TABLE IF NOT EXISTS work_entries (...)` per schema in design.md
3. Run `CREATE TABLE IF NOT EXISTS app_settings (key TEXT PRIMARY KEY, value TEXT NOT NULL)`
4. Seed default settings rows using `INSERT OR IGNORE` (so existing user data is never overwritten)
5. Expose `static string DbPath` and `static void Initialize()`  
**Done when:** calling `Initialize()` creates the file and tables; calling it twice is idempotent  
**Gate:** manual run + inspect DB with any SQLite viewer

---

## TASK-04 — WorkEntryRepository [P]
**What:** All SQLite CRUD for work entries  
**Where:** `Data/WorkEntryRepository.cs`  
**Depends on:** TASK-02, TASK-03  
**Methods:**
- `CreatePendingEntry(DateOnly date, TimeOnly startTime) → int` (returns new id)
- `CloseEntry(int id, TimeOnly endTime, string? description)` — sets end_time, total_hours
- `GetOpenSession() → WorkEntry?` — WHERE end_time IS NULL
- `AddEntry(WorkEntry entry)` — full insert (manual add)
- `GetEntriesByRange(DateOnly from, DateOnly to) → List<WorkEntry>`
- `UpdateEntry(WorkEntry entry)` — UPDATE by id
- `DeleteEntry(int id)`  
**Done when:** each method executes without exception against the real SQLite DB  
**Gate:** `dotnet build`

---

## TASK-05 — SettingsRepository [P]
**What:** Key-value SQLite settings persistence  
**Where:** `Data/SettingsRepository.cs`  
**Depends on:** TASK-02, TASK-03  
**Methods:**
- `GetAll() → AppSettings` — reads all keys, maps to model, applies defaults for missing keys
- `SaveAll(AppSettings settings)` — `INSERT OR REPLACE` for each key  
**Done when:** round-trip save + load returns identical values  
**Gate:** `dotnet build`

---

## TASK-06 — WindowTitleScanner [P]
**What:** P/Invoke `EnumWindows` to scan open window titles and extract ticket numbers  
**Where:** `Services/WindowTitleScanner.cs`  
**Depends on:** TASK-01  
**Steps:**
1. P/Invoke declarations: `EnumWindows`, `GetWindowText`, `IsWindowVisible`, `GetForegroundWindow`
2. `Scan() → string?`:
   a. Get foreground window handle → check its title first
   b. If no match, enumerate all visible windows
   c. Apply patterns: ADO `(?:AB)?#(\d+)`, Jira `\b([A-Z]{2,10}-\d+)\b`
   d. Return first match (raw ticket string), or null
3. Caller wraps result: `$"Are you still working on task #{ticket}?"`  
**Done when:** with an Azure DevOps browser tab open, `Scan()` returns the ticket number  
**Gate:** `dotnet build`; manual test with a browser window titled like `"#1234 · My Task"`

---

## TASK-07 — ScreenShareDetector [P]
**What:** Detect if Zoom or Teams is actively sharing the screen  
**Where:** `Services/ScreenShareDetector.cs`  
**Depends on:** TASK-01  
**Steps:**
1. P/Invoke: `FindWindow(lpClassName, lpWindowName)`, `EnumWindows`, `GetWindowThreadProcessId`, `GetClassName`
2. Zoom detection: `FindWindow("ZPToolbar", null) != IntPtr.Zero`
3. Teams detection: enumerate windows owned by `ms-teams.exe` processes; check window titles contain "sharing" or "apresentando" (PT-BR)
4. `bool IsSharing()` → returns true if either is detected
5. Wrap in try/catch — any exception → return false (fail open)
6. Add XML doc comment: "Google Meet and Webex not detected in v1"  
**Done when:** returns false when no sharing; tested manually by starting a Zoom share  
**Gate:** `dotnet build`

---

## TASK-08 — AutoStartService [P]
**What:** Manage Windows registry auto-start entry  
**Where:** `Services/AutoStartService.cs`  
**Depends on:** TASK-01  
**Steps:**
1. Registry key: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `"WorkTracker"`
2. `Enable()` → write current `Application.ExecutablePath`
3. `Disable()` → delete value if exists
4. `IsEnabled() → bool` → check value exists and matches current exe path  
**Done when:** after `Enable()`, app appears in Task Manager startup list  
**Gate:** `dotnet build`

---

## TASK-09 — CsvExportService [P]
**What:** Export a list of work entries to a CSV file with Artia headers  
**Where:** `Services/CsvExportService.cs`  
**Depends on:** TASK-02, TASK-05  
**Steps:**
1. Create CsvHelper `ClassMap<WorkEntry>` with Portuguese column names:
   - `Data` → `Date.ToString("dd/MM/yyyy")`
   - `Hora de início` → `StartTime.ToString("HH:mm")`
   - `Esforço` → `TotalHours?.ToString("0.##")`
   - `Observação` → `Description`
   - `ID da Atividade` → injected from `AppSettings.ArtiaActivityId`
   - `E-mail` → injected from `AppSettings.UserEmail`
2. `Export(IEnumerable<WorkEntry> entries, AppSettings settings, string filePath)`
3. Use UTF-8 with BOM (Excel opens correctly without re-encoding step)  
**Done when:** exported CSV opens in Excel with correct columns and PT-BR headers  
**Gate:** `dotnet build`; manual open in Excel

---

## TASK-10 — PromptScheduler
**What:** Background timer that fires daily prompts at random times within configured windows  
**Where:** `App/PromptScheduler.cs`  
**Depends on:** TASK-05, TASK-07  
**Steps:**
1. `System.Threading.Timer` ticking every 30 seconds
2. On start: calculate `_nextFire1` and `_nextFire2` (random time within each window for today; if already past, schedule for tomorrow)
3. On each tick:
   a. Skip if `DateTime.Now.DayOfWeek` is Saturday or Sunday
   b. For each fire time: if `DateTime.Now >= fireTime` and not suppressed by delay:
      - Call `ScreenShareDetector.IsSharing()`
      - If sharing → skip, retry next tick
      - If not sharing → raise `PromptDue` event, recalculate fire time for tomorrow
   c. Handle delay: if `_delayUntil` is set and `DateTime.Now < _delayUntil` → skip
4. `public void Delay(int minutes)` → set `_delayUntil = DateTime.Now.AddMinutes(minutes)`; fire after delay regardless of window end
5. `public void Restart(AppSettings settings)` → reload windows and recalculate (called after Settings save)
6. Raise event on UI thread: `form.Invoke(() => PromptDue?.Invoke(...))`  
**Done when:** prompt fires within the configured window on a weekday; delayed prompt fires after delay even if window has closed  
**Gate:** `dotnet build`; manual test by temporarily setting window to ±2 min from now

---

## TASK-11 — StartWorkForm [P]
**What:** Small dialog to record work session start time  
**Where:** `Forms/StartWorkForm.cs`  
**Depends on:** TASK-04  
**Layout:** 360×170px, no resize, topmost  
**Controls:** Label ("What time did you start working?"), `DateTimePicker` (Format=Time, Value=Now), Button "Start", Button "Cancel"  
**Logic:**
- On "Start": check `WorkEntryRepository.GetOpenSession()` — if exists, show `MessageBox` ("You already have an open session. Finish it first.")
- If no open session: call `WorkEntryRepository.CreatePendingEntry(today, selectedTime)` → close  
**Done when:** clicking Start creates a pending DB row; duplicate guard works  
**Gate:** `dotnet build`; manual smoke test

---

## TASK-12 — FinishWorkForm [P]
**What:** Dialog to close an open session with end time and description  
**Where:** `Forms/FinishWorkForm.cs`  
**Depends on:** TASK-04, TASK-06  
**Layout:** 420×240px, no resize, topmost  
**Controls:** Label ("What time did you finish?"), `DateTimePicker` (Format=Time, Value=Now), TextBox (Description, pre-filled from WindowTitleScanner), Label "Total: X h" (live update), Button "Save", Button "Cancel"  
**Logic:**
- On load: run `WindowTitleScanner.Scan()` → pre-fill description
- Live total: `endTime - openSession.StartTime` → format as `h:mm` or decimal
- On "Save": `WorkEntryRepository.CloseEntry(session.Id, endTime, description)` → close  
**Done when:** saving closes the pending row with correct total_hours calculation  
**Gate:** `dotnet build`; manual test with an open session in DB

---

## TASK-13 — PromptForm [P]
**What:** Automated daily prompt popup  
**Where:** `Forms/PromptForm.cs`  
**Depends on:** TASK-04, TASK-06, TASK-10  
**Layout:** 420×230px, no resize, topmost, centered on screen  
**Controls:** Label ("What are you working on?"), `DateTimePicker` (time, Value=Now), TextBox (Description, pre-filled), Button "Confirm", ComboBox ["15 min","30 min","60 min"] + Button "Delay"  
**Logic:**
- On load: `WindowTitleScanner.Scan()` → pre-fill description
- Confirm: `WorkEntryRepository.CreatePendingEntry(date, startTime)` → close with `DialogResult.OK`
- Delay: parse selected minutes → call `PromptScheduler.Delay(minutes)` → close with `DialogResult.Cancel`
- `FormClosing` (X button): `PromptScheduler.Delay(15)` → cancel close normally  
**Done when:** Confirm saves a pending entry; Delay triggers scheduler delay; X acts as 15-min delay  
**Gate:** `dotnet build`; manual trigger via scheduler test shortcut

---

## TASK-14 — AddEntryForm [P]
**What:** Manual entry dialog for adding complete work entries  
**Where:** `Forms/AddEntryForm.cs`  
**Depends on:** TASK-04  
**Layout:** 420×290px, no resize  
**Controls:** `DateTimePicker` (date, Value=Today), `DateTimePicker` (start time), `DateTimePicker` (end time), TextBox (Description), Label "Total: X h" (live), Button "Save", Button "Cancel"  
**Logic:**
- Live total: `endTime - startTime` → update label on time change events
- Validate: end > start before saving
- On "Save": `WorkEntryRepository.AddEntry(new WorkEntry{...})` → close  
**Done when:** saved entry appears in DB with correct total_hours  
**Gate:** `dotnet build`; manual smoke test

---

## TASK-15 — SettingsForm [P]
**What:** Settings dialog for configuring prompt windows, Artia ID, and email  
**Where:** `Forms/SettingsForm.cs`  
**Depends on:** TASK-05, TASK-08, TASK-10  
**Layout:** 400×330px, no resize  
**Controls:**
- Section "Prompt Windows": 2 rows of `DateTimePicker(start)` → `DateTimePicker(end)` for Window 1 and 2
- Section "Artia Export": TextBox (ID da Atividade), TextBox (E-mail)
- Button "Save", Button "Cancel"  
**Logic:**
- On load: `SettingsRepository.GetAll()` → populate fields
- On "Save": `SettingsRepository.SaveAll(settings)` → `PromptScheduler.Restart(settings)` → close  
**Done when:** changes persist across app restarts; scheduler reloads immediately  
**Gate:** `dotnet build`; manual save + reopen settings to verify persistence

---

## TASK-16 — WeekViewForm
**What:** Main window — table of entries with week navigation, inline edit, delete, export  
**Where:** `Forms/WeekViewForm.cs`  
**Depends on:** TASK-04, TASK-09, TASK-14  
**Layout:** 820×520px, resizable, singleton  
**Controls:**
- Top bar: Button "◀", Label (week range), Button "▶", DateTimePicker (from), DateTimePicker (to), Button "Apply"
- `DataGridView`: columns Date | Start | End | Hours | Description | [Delete btn]
- Bottom bar: Button "Export", Button "Add Entry", Label "Week total: X h"  
**Logic:**
- Default view: current Mon–Sun week
- Prev/Next: shift week by 7 days, reload
- Custom range: Apply button loads entries for picker range
- Grouping: sort by date; insert subtotal row after each date group (non-editable, bold)
- Insert week total row at bottom (non-editable, bold)
- Inline edit: `DataGridView` in edit mode; on `CellEndEdit` → `WorkEntryRepository.UpdateEntry(...)`; recalculate totals
- Delete: per-row button → `MessageBox.Confirm` → `WorkEntryRepository.DeleteEntry(id)` → reload
- Export: `SaveFileDialog` → `CsvExportService.Export(entries, settings, path)`
- Add Entry: `new AddEntryForm().ShowDialog()` → reload grid on OK  
**Done when:** entries display correctly grouped by day; edit and delete persist; export produces valid CSV  
**Gate:** `dotnet build`; manual end-to-end with test data

---

## TASK-17 — TrayApplicationContext + Program.cs
**What:** Wire everything together — tray icon, menu, form instances, scheduler  
**Where:** `App/TrayApplicationContext.cs`, `Program.cs`  
**Depends on:** TASK-10, TASK-11, TASK-12, TASK-13, TASK-14, TASK-15, TASK-16  
**Steps:**
1. `Program.cs`:
   - `DatabaseInitializer.Initialize()`
   - `AutoStartService.Enable()` on first run (check `IsEnabled()` first)
   - `Application.Run(new TrayApplicationContext())`
2. `TrayApplicationContext`:
   - Create `NotifyIcon` with `ContextMenuStrip` (8 items per spec REQ-001)
   - Load tray icon from `Resources/tray.ico`
   - Instantiate `PromptScheduler`, subscribe to `PromptDue` event
   - `PromptDue` handler: `Invoke(() => new PromptForm(scheduler).ShowDialog())`
   - Hold singleton `WeekViewForm _weekView`; Open/ViewWeek → create if null, else `.BringToFront()`
   - Wire each menu item to its form/action
   - `Exit` handler: dispose `NotifyIcon`, stop scheduler, `Application.Exit()`  
**Done when:** app starts to tray, all menu items open correct forms, prompts fire on schedule, Exit cleans up  
**Gate:** `dotnet build`; full manual walkthrough of all 8 menu items

---

## TASK-18 — Tray icon asset
**What:** Create a simple tray icon (.ico, 16×16 and 32×32)  
**Where:** `Resources/tray.ico`  
**Depends on:** TASK-01  
**Steps:**
1. Create a minimal clock/timer icon (can use any free icon tool or generate programmatically)
2. Embed as project resource (`<EmbeddedResource>` in .csproj)
3. Reference in `TrayApplicationContext` via `new Icon(typeof(TrayApplicationContext), "Resources.tray.ico")`  
**Done when:** tray icon appears (not blank/broken) in the Windows system tray  
**Gate:** visual check in system tray

---

## TASK-19 — End-to-end smoke test
**What:** Full manual walkthrough of the happy path and key edge cases  
**Where:** running app  
**Depends on:** TASK-17, TASK-18  
**Checklist:**
- [ ] App starts, icon appears in tray, no console window
- [ ] Start Work → creates pending entry in DB
- [ ] Finish Work → closes entry, total_hours correct
- [ ] Add Entry → manual entry saved correctly
- [ ] Open/View Week → entries displayed, day totals correct, week total correct
- [ ] Inline edit → change saved on cell leave
- [ ] Delete → row removed after confirm
- [ ] Export → CSV opens in Excel with Portuguese headers, data correct
- [ ] Settings → change prompt window, save, reopen to verify persistence
- [ ] Prompt fires at scheduled time (test with near-future window)
- [ ] Delay prompt → fires after delay
- [ ] X on prompt → treated as 15-min delay
- [ ] Weekend check → no prompt on Saturday/Sunday (verify scheduler skips)
- [ ] Window title scan → ADO/Jira ticket pre-fills description
- [ ] Auto-start → appears in Task Manager startup tab  
**Done when:** all checklist items pass  
**Gate:** manual checklist signed off

---

## Execution Order

```
TASK-01 (scaffold)
  ├── TASK-02 [P] Models
  ├── TASK-03 [P] DB Initializer
  ├── TASK-06 [P] WindowTitleScanner
  ├── TASK-07 [P] ScreenShareDetector
  └── TASK-08 [P] AutoStartService
        ↓ (TASK-02 + TASK-03 complete)
  ├── TASK-04 [P] WorkEntryRepository
  └── TASK-05 [P] SettingsRepository
        ↓ (TASK-04 + TASK-05 complete)
  ├── TASK-09 [P] CsvExportService
  └── TASK-10    PromptScheduler
        ↓ (all services + repositories complete)
  ├── TASK-11 [P] StartWorkForm
  ├── TASK-12 [P] FinishWorkForm
  ├── TASK-13 [P] PromptForm
  ├── TASK-14 [P] AddEntryForm
  ├── TASK-15 [P] SettingsForm
  └── TASK-18 [P] Tray icon asset
        ↓
  TASK-16    WeekViewForm
        ↓
  TASK-17    TrayApplicationContext + Program.cs
        ↓
  TASK-19    End-to-end smoke test
```
