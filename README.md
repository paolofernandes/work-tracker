# Work Tracker

A Windows system tray app that prompts you twice a day to log what you're working on, so you can export your hours to Artia at the end of the week without guessing.

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Windows | 11 (x64) |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 or later |

No other dependencies — SQLite and all NuGet packages are restored automatically on first build.

---

## How to Run

### 1. Clone or download the project

```
git clone <repo-url>
cd WorkTracker
```

### 2. Build and run

```
cd WorkTracker
dotnet run
```

The app will appear in the **system tray** (bottom-right, near the clock). There is no main window — everything is accessed by right-clicking the tray icon.

### 3. First-time setup

Right-click the tray icon → **Settings** and fill in:

- **ID da Atividade** — your Artia activity ID for this month (changes monthly)
- **E-mail** — your Artia email (pre-filled with `paolo.fernandes@lyncas.net`)
- **Prompt Windows** — times when the daily prompts fire (default: 10–11 am and 3–4 pm)

---

## How to Build a Release Executable

```
cd WorkTracker
dotnet publish -c Release -r win-x64 --self-contained false -o ../publish
```

The output in `../publish/WorkTracker.exe` can be copied anywhere on the machine. On first run it registers itself in Windows startup automatically.

---

## Usage

### Tray menu

| Item | What it does |
|------|--------------|
| **Open** | Opens the week view window |
| **Start Work** | Records what time you started a work session |
| **Add Entry** | Manually add a complete entry (date, start, end, description) |
| **View Week** | Opens the week view window |
| **Export** | Exports the current week to CSV (Artia format) |
| **Finish Work** | Records end time, calculates hours, saves the entry |
| **Settings** | Configure prompt times, Artia ID, and email |
| **Exit** | Closes the app |

### Daily prompts

At a random time within each configured window, a popup appears asking what you're working on. If you have an Azure DevOps or Jira tab open in the foreground, the task number is pre-filled.

- **Confirm** — saves the entry
- **Delay** — snoozes for 15, 30, or 60 minutes
- **✕ (close)** — automatically snoozes for 15 minutes

Prompts only fire on **weekdays (Monday–Friday)**. If your screen is being shared via Zoom or Teams, the prompt waits until sharing stops.

### Week view

Double-click any cell (or press F2) to edit it inline. Changes are saved immediately. Use the **◀ ▶** arrows to navigate weeks, or pick a custom date range with the pickers and click **Apply**.

### Exporting to Artia

1. Right-click tray icon → **Export** (exports current week), or open the week view and click **Export CSV**
2. Pick a save location
3. Upload the CSV to Artia

The CSV columns match Artia's format exactly:

```
Data, Hora de início, Esforço, Observação, ID da Atividade, E-mail
```

> **Remember:** Update **ID da Atividade** in Settings at the start of each month.

---

## Data Storage

All data is stored locally in a SQLite database at:

```
%APPDATA%\WorkTracker\worktracker.db
```

Nothing is sent to any server. Back up this file to preserve your history.

---

## Known Limitations

- Screen sharing detection covers **Zoom** and **Microsoft Teams** only. Google Meet and Webex shares will not delay prompts.
- The tray icon is a placeholder — replace `WorkTracker/Resources/tray.ico` with a custom icon and rebuild.
