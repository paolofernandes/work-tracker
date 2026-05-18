using WorkTracker.Data;
using WorkTracker.Models;
using WorkTracker.Services;

namespace WorkTracker.Forms;

class WeekViewForm : Form
{
    readonly DataGridView _grid;
    readonly Label _weekLabel;
    readonly DateTimePicker _fromPicker, _toPicker;
    DateOnly _rangeFrom, _rangeTo;

    // Row tag types
    record EntryRow(WorkEntry Entry);
    record DayTotalRow();
    record WeekTotalRow();

    public WeekViewForm()
    {
        Text = "Work Tracker — Week View";
        Size = new Size(840, 540);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterScreen;

        // ── Top bar ──────────────────────────────────────────────
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8, 8, 8, 4) };

        var btnPrev = new Button { Text = "◀", Size = new Size(36, 28), Location = new Point(8, 10) };
        btnPrev.Click += (_, _) => ShiftWeek(-7);

        _weekLabel = new Label
        {
            Location = new Point(52, 14),
            Size = new Size(200, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold)
        };

        var btnNext = new Button { Text = "▶", Size = new Size(36, 28), Location = new Point(258, 10) };
        btnNext.Click += (_, _) => ShiftWeek(7);

        var lblFrom = new Label { Text = "From:", Location = new Point(316, 14), Size = new Size(38, 20) };
        _fromPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(356, 10),
            Size = new Size(110, 28)
        };

        var lblTo = new Label { Text = "To:", Location = new Point(474, 14), Size = new Size(28, 20) };
        _toPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(504, 10),
            Size = new Size(110, 28)
        };

        var btnApply = new Button { Text = "Apply", Location = new Point(622, 10), Size = new Size(60, 28) };
        btnApply.Click += (_, _) =>
        {
            _rangeFrom = DateOnly.FromDateTime(_fromPicker.Value);
            _rangeTo   = DateOnly.FromDateTime(_toPicker.Value);
            LoadData();
        };

        topPanel.Controls.AddRange([btnPrev, _weekLabel, btnNext, lblFrom, _fromPicker, lblTo, _toPicker, btnApply]);

        // ── DataGridView ─────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EditMode = DataGridViewEditMode.EditOnF2,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9f)
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Date",        HeaderText = "Date",        FillWeight = 90,  ReadOnly = false },
            new DataGridViewTextBoxColumn { Name = "Start",       HeaderText = "Start",       FillWeight = 60,  ReadOnly = false },
            new DataGridViewTextBoxColumn { Name = "End",         HeaderText = "End",         FillWeight = 60,  ReadOnly = false },
            new DataGridViewTextBoxColumn { Name = "Hours",       HeaderText = "Hours",       FillWeight = 55,  ReadOnly = true  },
            new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 220, ReadOnly = false },
            new DataGridViewButtonColumn  { Name = "Delete",      HeaderText = "",            FillWeight = 40,  Text = "✕", UseColumnTextForButtonValue = true }
        );

        _grid.CellEndEdit          += OnCellEndEdit;
        _grid.CellClick            += OnCellClick;
        _grid.CellBeginEdit        += OnCellBeginEdit;
        _grid.RowPrePaint          += OnRowPrePaint;

        // ── Bottom bar ───────────────────────────────────────────
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8, 6, 8, 6) };

        var btnAdd = new Button { Text = "Add Entry", Location = new Point(8, 8), Size = new Size(90, 28) };
        btnAdd.Click += (_, _) =>
        {
            if (new AddEntryForm().ShowDialog() == DialogResult.OK) LoadData();
        };

        var btnExport = new Button { Text = "Export CSV", Location = new Point(108, 8), Size = new Size(90, 28) };
        btnExport.Click += OnExport;

        bottomPanel.Controls.AddRange([btnAdd, btnExport]);

        Controls.AddRange([topPanel, _grid, bottomPanel]);

        SetCurrentWeek();
    }

    // ── Range helpers ────────────────────────────────────────────

    void SetCurrentWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        _rangeFrom = today.AddDays(-daysFromMonday);
        _rangeTo   = _rangeFrom.AddDays(6);
        SyncPickers();
        LoadData();
    }

    void ShiftWeek(int days)
    {
        _rangeFrom = _rangeFrom.AddDays(days);
        _rangeTo   = _rangeTo.AddDays(days);
        SyncPickers();
        LoadData();
    }

    void SyncPickers()
    {
        _fromPicker.Value = _rangeFrom.ToDateTime(TimeOnly.MinValue);
        _toPicker.Value   = _rangeTo.ToDateTime(TimeOnly.MinValue);
        _weekLabel.Text   = $"{_rangeFrom:dd MMM} – {_rangeTo:dd MMM yyyy}";
    }

    // ── Data loading ─────────────────────────────────────────────

    public void LoadData_Public() => LoadData();

    void LoadData()
    {
        _grid.Rows.Clear();

        var entries = WorkEntryRepository.GetEntriesByRange(_rangeFrom, _rangeTo);
        var groups  = entries.GroupBy(e => e.Date).OrderBy(g => g.Key);

        double weekTotal = 0;

        foreach (var group in groups)
        {
            double dayTotal = 0;
            bool firstInGroup = true;

            foreach (var entry in group.OrderBy(e => e.StartTime))
            {
                var row = new DataGridViewRow();
                row.CreateCells(_grid,
                    firstInGroup ? entry.Date.ToString("ddd dd/MM") : "",
                    entry.StartTime.ToString("HH:mm"),
                    entry.EndTime?.ToString("HH:mm") ?? "—",
                    entry.TotalHours?.ToString("0.##") ?? "—",
                    entry.Description ?? "");
                row.Tag = new EntryRow(entry);
                _grid.Rows.Add(row);

                dayTotal += entry.TotalHours ?? 0;
                weekTotal += entry.TotalHours ?? 0;
                firstInGroup = false;
            }

            // Day subtotal row
            var subtotal = new DataGridViewRow();
            subtotal.CreateCells(_grid, "", "", "Day total", dayTotal.ToString("0.##") + "h", "");
            subtotal.Tag = new DayTotalRow();
            subtotal.ReadOnly = true;
            _grid.Rows.Add(subtotal);
        }

        // Week total row
        if (_grid.Rows.Count > 0)
        {
            var total = new DataGridViewRow();
            total.CreateCells(_grid, "", "", "Week total", weekTotal.ToString("0.##") + "h", "");
            total.Tag = new WeekTotalRow();
            total.ReadOnly = true;
            _grid.Rows.Add(total);
        }
    }

    // ── Grid event handlers ──────────────────────────────────────

    void OnRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        var row = _grid.Rows[e.RowIndex];
        if (row.Tag is DayTotalRow)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 255);
            row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
            row.DefaultCellStyle.ForeColor = Color.DarkSlateBlue;
        }
        else if (row.Tag is WeekTotalRow)
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(210, 235, 255);
            row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
            row.DefaultCellStyle.ForeColor = Color.Navy;
        }
    }

    void OnCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        var row = _grid.Rows[e.RowIndex];
        // Only allow editing on entry rows, and not the Delete column
        if (row.Tag is not EntryRow || e.ColumnIndex == _grid.Columns["Delete"]!.Index)
            e.Cancel = true;
    }

    void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var row = _grid.Rows[e.RowIndex];
        if (row.Tag is not EntryRow(var entry)) return;

        var col  = _grid.Columns[e.ColumnIndex].Name;
        var text = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

        switch (col)
        {
            case "Start":
                if (TimeOnly.TryParse(text, out var newStart))
                    entry.StartTime = newStart;
                break;
            case "End":
                if (TimeOnly.TryParse(text, out var newEnd))
                {
                    entry.EndTime   = newEnd;
                    entry.TotalHours = Math.Round((newEnd - entry.StartTime).TotalHours, 2);
                }
                break;
            case "Description":
                entry.Description = text;
                break;
            case "Date":
                if (DateOnly.TryParse(text, out var newDate))
                    entry.Date = newDate;
                break;
        }

        WorkEntryRepository.UpdateEntry(entry);
        // Reload to recalculate all totals cleanly
        BeginInvoke(LoadData);
    }

    void OnCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_grid.Columns[e.ColumnIndex].Name != "Delete") return;

        var row = _grid.Rows[e.RowIndex];
        if (row.Tag is not EntryRow(var entry)) return;

        var confirm = MessageBox.Show(
            $"Delete entry on {entry.Date:dd/MM/yyyy} ({entry.StartTime}–{entry.EndTime})?",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm == DialogResult.Yes)
        {
            WorkEntryRepository.DeleteEntry(entry.Id);
            LoadData();
        }
    }

    // ── Export ───────────────────────────────────────────────────

    void OnExport(object? sender, EventArgs e)
    {
        var entries  = WorkEntryRepository.GetEntriesByRange(_rangeFrom, _rangeTo)
                           .Where(en => en.TotalHours.HasValue)
                           .ToList();
        var settings = SettingsRepository.GetAll();

        using var dlg = new SaveFileDialog
        {
            Title      = "Export to CSV",
            Filter     = "CSV files (*.csv)|*.csv",
            FileName   = $"WorkTracker_{_rangeFrom:yyyy-MM-dd}_{_rangeTo:yyyy-MM-dd}.csv",
            DefaultExt = "csv"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            CsvExportService.Export(entries, settings, dlg.FileName);
            MessageBox.Show($"Exported {entries.Count} entries to:\n{dlg.FileName}",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
