# Feature Spec: Work Time Tracker

**Scope:** Large — multi-component, requires Design and Tasks phases  
**Status:** Specified

---

## Overview

A Windows 11 system tray application that prompts the user twice daily to log work entries, supports manual entry, displays a scrollable history, and exports to CSV for Artia upload.

---

## Requirements

### REQ-001 — Tray Application Shell
- The app runs as a Windows system tray icon at all times
- Auto-starts with Windows login (startup registry entry or Task Scheduler)
- Right-click tray icon reveals context menu with items (in order):
  1. Open
  2. Start Work *(prompts for start time)*
  3. Add Entry *(manual form)*
  4. View Week
  5. Export
  6. Finish Work *(prompts for end time, calculates hours)*
  7. Settings
  8. Exit

### REQ-002 — Work Entry Data Model
Each entry stores:
| Field        | Type     | Notes                          |
|--------------|----------|--------------------------------|
| id           | integer  | Primary key                    |
| date         | date     | Day the work occurred          |
| start_time   | time     | User-entered start             |
| end_time     | time     | User-entered end               |
| total_hours  | decimal  | Calculated: end - start        |
| description  | text     | Free text, typically task #    |
| created_at   | datetime | Audit timestamp                |

- Multiple entries per day are supported
- All data stored in a local SQLite database

### REQ-003 — Daily Prompts (Automated)
- Prompts fire on **workdays only (Monday–Friday)**; no prompts on weekends
- Two prompts fire per day at a **random time within configurable windows**:
  - Default Window 1: 10:00–11:00
  - Default Window 2: 15:00–16:00
- Each prompt opens a popup form asking:
  - Start time of this work block (text input, pre-filled with current time as suggestion)
  - Description / task number (text input, see REQ-006 for suggestion)
- Popup has three actions:
  - **Confirm** — saves the entry start; end time is set when user triggers Finish Work
  - **Delay** — snoozes; user picks 15, 30, or 60 minutes from a dropdown
  - *(implicit close is treated as Delay 15 min)*
- If delay pushes past the window end time, the prompt fires after the delay anyway (does not skip)
- **Screen sharing detection:** if the user's screen is being shared at prompt time, hold the prompt until sharing stops, then show it

### REQ-004 — Window Title Task Detection
- When a prompt opens (automated or manual), scan open window titles on the desktop
- Detect ticket numbers using these patterns:
  - Azure DevOps: `#\d+` or `AB#\d+`
  - Jira: `[A-Z]{2,10}-\d+` (e.g. `PROJ-123`)
- If a match is found, pre-fill the description field with:
  `"Are you still working on task #[ticket]?"`
- If multiple matches found, use the most recently focused window's ticket
- If no match, description field starts empty

### REQ-005 — Start Work
- Tray menu "Start Work" opens a small form:
  - "What time did you start?" (time picker, defaults to current time)
  - Confirm button
- Records the session start time in memory/DB as a pending entry (no end time yet)
- Only one open session at a time; if one exists, prompt to close it first

### REQ-006 — Finish Work
- Tray menu "Finish Work" opens a small form:
  - "What time did you finish?" (time picker, defaults to current time)
  - Description field (with window title detection, REQ-004)
  - Confirm button
- App calculates `total_hours = end_time - start_time`
- Saves the entry to SQLite
- Multiple Start Work → Finish Work cycles per day = multiple entries (correct behavior)

### REQ-007 — Manual Add Entry
- Tray menu "Add Entry" opens a form:
  - Date (date picker, defaults to today)
  - Start time (time picker)
  - End time (time picker)
  - Description (text input)
  - Total hours shown as calculated preview (live, updates as times change)
  - Save button

### REQ-008 — Week / Date Range View
- Opens a full window (from tray "Open" or "View Week")
- Displays all entries as a table, one row per entry:
  | Date | Start | End | Hours | Description |
- Navigation:
  - Previous week / Next week arrows
  - Date range picker (from → to) for custom ranges
- Each row supports:
  - Inline edit (click to edit any field)
  - Delete (with confirmation)
- Shows a **daily total** row at the bottom of each day's group
- Shows a **weekly total** at the bottom of the view

### REQ-009 — Export to CSV
- Accessible from tray menu "Export" or from the week view
- User selects date range (defaults to current week)
- Exports CSV with exact Artia column headers (in Portuguese):
  `Data, Hora de início, Esforço, Observação, ID da Atividade, E-mail`
- Field mapping:
  | CSV Column      | Source                                      |
  |-----------------|---------------------------------------------|
  | Data            | entry date (dd/MM/yyyy)                     |
  | Hora de início  | entry start time (HH:mm)                    |
  | Esforço         | total hours (decimal, e.g. 3.5)             |
  | Observação      | description / task number                   |
  | ID da Atividade | configurable monthly value (see REQ-010)    |
  | E-mail          | fixed user email from settings (see REQ-010)|
- User picks save location via standard Windows file dialog

### REQ-010 — Settings
- Settings screen accessible from tray menu "Settings"
- Configurable options:
  | Setting           | Default                        | Notes                          |
  |-------------------|--------------------------------|--------------------------------|
  | Prompt Window 1   | 10:00–11:00                    | Start and end time             |
  | Prompt Window 2   | 15:00–16:00                    | Start and end time             |
  | ID da Atividade   | *(empty — user sets monthly)*  | Changes every month for Artia  |
  | E-mail            | paolo.fernandes@lyncas.net     | Used in CSV export             |
- Changes persist immediately to app config (SQLite config table or JSON file)

---

## Open Questions / Deferred

| # | Question | Decision |
|---|----------|----------|
| OQ-1 | Exact Artia CSV column names and format | **Resolved** — see REQ-009 |
| OQ-2 | Screen sharing detection API on Windows (DXGI / UIA) | To be confirmed in Design phase |
| OQ-3 | App name / icon | Not decided |
| OQ-4 | Tray icon approach | **Resolved** — WinForms `NotifyIcon` (native, no interop needed) |

---

## Out of Scope (v1)
- Artia API integration
- Notifications beyond the prompt popup
- Dark/light theme toggle
- Reporting or charts
- Cloud sync or backup
