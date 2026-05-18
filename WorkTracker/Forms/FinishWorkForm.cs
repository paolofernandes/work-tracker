using WorkTracker.Data;
using WorkTracker.Models;
using WorkTracker.Services;

namespace WorkTracker.Forms;

class FinishWorkForm : Form
{
    readonly DateTimePicker _timePicker;
    readonly TextBox _description;
    readonly Label _totalLabel;
    readonly WorkEntry _session;

    public FinishWorkForm(WorkEntry session)
    {
        _session = session;

        Text = "Finish Work";
        Size = new Size(420, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        var lblTime = new Label
        {
            Text = "What time did you finish?",
            Location = new Point(16, 16),
            Size = new Size(280, 20),
            Font = new Font(Font.FontFamily, 10f)
        };

        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Now,
            Location = new Point(16, 42),
            Size = new Size(120, 28)
        };
        _timePicker.ValueChanged += (_, _) => UpdateTotal();

        _totalLabel = new Label
        {
            Text = "Total: —",
            Location = new Point(150, 46),
            Size = new Size(240, 20),
            ForeColor = Color.DimGray
        };

        var lblDesc = new Label
        {
            Text = "Description / Task #",
            Location = new Point(16, 82),
            Size = new Size(200, 20)
        };

        _description = new TextBox
        {
            Location = new Point(16, 104),
            Size = new Size(376, 24),
            Text = BuildSuggestion(WindowTitleScanner.Scan())
        };

        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(216, 148),
            Size = new Size(80, 30)
        };
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(308, 148),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Controls.AddRange([lblTime, _timePicker, _totalLabel, lblDesc, _description, btnSave, btnCancel]);

        UpdateTotal();
    }

    void UpdateTotal()
    {
        var end = TimeOnly.FromDateTime(_timePicker.Value);
        var diff = end - _session.StartTime;
        _totalLabel.Text = diff.TotalMinutes > 0
            ? $"Total: {diff.Hours}h {diff.Minutes}m ({diff.TotalHours:0.##}h)"
            : "Total: —";
    }

    void OnSave(object? sender, EventArgs e)
    {
        var end = TimeOnly.FromDateTime(_timePicker.Value);
        if (end <= _session.StartTime)
        {
            MessageBox.Show("End time must be after start time.", "Invalid Time",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        WorkEntryRepository.CloseEntry(_session.Id, end, _description.Text.Trim());
        DialogResult = DialogResult.OK;
        Close();
    }

    static string BuildSuggestion(string? ticket) =>
        ticket is null ? string.Empty : $"Are you still working on task #{ticket}?";
}
