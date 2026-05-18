using WorkTracker.Data;
using WorkTracker.Forms;
using WorkTracker.Services;

namespace WorkTracker.App;

class TrayApplicationContext : ApplicationContext
{
    readonly NotifyIcon _trayIcon;
    readonly PromptScheduler _scheduler;
    WeekViewForm? _weekView;

    public TrayApplicationContext()
    {
        // Auto-start on first run
        if (!AutoStartService.IsEnabled())
            AutoStartService.Enable();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open",        null, (_, _) => ShowWeekView());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Start Work",  null, (_, _) => ShowStartWork());
        menu.Items.Add("Add Entry",   null, (_, _) => ShowAddEntry());
        menu.Items.Add("View Week",   null, (_, _) => ShowWeekView());
        menu.Items.Add("Export",      null, (_, _) => ShowExport());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Finish Work", null, (_, _) => ShowFinishWork());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings",    null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit",        null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon             = LoadTrayIcon(),
            Text             = "Work Tracker",
            ContextMenuStrip = menu,
            Visible          = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWeekView();

        // Invisible handle owner for cross-thread Invoke in PromptScheduler
        var invokeHost = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
        invokeHost.Load += (_, _) => invokeHost.Hide();
        invokeHost.Show();

        _scheduler = new PromptScheduler(invokeHost);
        _scheduler.PromptDue += (_, _) => ShowPrompt();
    }

    // ── Menu handlers ────────────────────────────────────────────

    void ShowWeekView()
    {
        if (_weekView is null || _weekView.IsDisposed)
        {
            _weekView = new WeekViewForm();
            _weekView.FormClosed += (_, _) => _weekView = null;
        }
        _weekView.Show();
        _weekView.BringToFront();
        _weekView.WindowState = FormWindowState.Normal;
    }

    void ShowStartWork()
    {
        using var form = new StartWorkForm();
        form.ShowDialog();
    }

    void ShowAddEntry()
    {
        using var form = new AddEntryForm();
        if (form.ShowDialog() == DialogResult.OK)
            _weekView?.Invoke(() => _weekView?.LoadData_Public());
    }

    void ShowFinishWork()
    {
        var session = WorkEntryRepository.GetOpenSession();
        if (session is null)
        {
            MessageBox.Show("No active work session.\nUse 'Start Work' first.",
                "No Session", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new FinishWorkForm(session);
        if (form.ShowDialog() == DialogResult.OK)
            _weekView?.Invoke(() => _weekView?.LoadData_Public());
    }

    void ShowSettings()
    {
        using var form = new SettingsForm(_scheduler);
        form.ShowDialog();
    }

    void ShowPrompt()
    {
        using var form = new PromptForm(_scheduler);
        if (form.ShowDialog() == DialogResult.OK)
            _weekView?.Invoke(() => _weekView?.LoadData_Public());
    }

    void ShowExport()
    {
        // Open week view and trigger export, or run export standalone
        var entries  = WorkEntryRepository.GetEntriesByRange(CurrentWeekFrom(), CurrentWeekTo())
                           .Where(e => e.TotalHours.HasValue).ToList();
        var settings = Data.SettingsRepository.GetAll();

        using var dlg = new SaveFileDialog
        {
            Title      = "Export to CSV",
            Filter     = "CSV files (*.csv)|*.csv",
            FileName   = $"WorkTracker_{CurrentWeekFrom():yyyy-MM-dd}.csv",
            DefaultExt = "csv"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            Services.CsvExportService.Export(entries, settings, dlg.FileName);
            MessageBox.Show($"Exported {entries.Count} entries.", "Done",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void ExitApp()
    {
        _scheduler.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Helpers ──────────────────────────────────────────────────

    static DateOnly CurrentWeekFrom()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
    }

    static DateOnly CurrentWeekTo() => CurrentWeekFrom().AddDays(6);

    static Icon LoadTrayIcon()
    {
        try
        {
            var stream = typeof(TrayApplicationContext).Assembly
                .GetManifestResourceStream("WorkTracker.Resources.tray.ico");
            if (stream is not null) return new Icon(stream);
        }
        catch { }
        return SystemIcons.Application;
    }
}
