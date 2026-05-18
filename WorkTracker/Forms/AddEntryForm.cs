using WorkTracker.Data;
using WorkTracker.Models;

namespace WorkTracker.Forms;

class AddEntryForm : Form
{
    readonly DateTimePicker _datePicker;
    readonly DateTimePicker _startPicker;
    readonly DateTimePicker _endPicker;
    readonly TextBox _description;
    readonly Label _totalLabel;

    public AddEntryForm()
    {
        Text = "Add Entry";
        Size = new Size(420, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var lblDate = new Label { Text = "Date:", Location = new Point(16, 20), Size = new Size(60, 20) };
        _datePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            Location = new Point(80, 16),
            Size = new Size(150, 28)
        };

        var lblStart = new Label { Text = "Start:", Location = new Point(16, 58), Size = new Size(60, 20) };
        _startPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(9),
            Location = new Point(80, 54),
            Size = new Size(110, 28)
        };
        _startPicker.ValueChanged += (_, _) => UpdateTotal();

        var lblEnd = new Label { Text = "End:", Location = new Point(16, 96), Size = new Size(60, 20) };
        _endPicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(10),
            Location = new Point(80, 92),
            Size = new Size(110, 28)
        };
        _endPicker.ValueChanged += (_, _) => UpdateTotal();

        _totalLabel = new Label
        {
            Text = "Total: 1h 0m",
            Location = new Point(204, 96),
            Size = new Size(190, 20),
            ForeColor = Color.DimGray
        };

        var lblDesc = new Label { Text = "Description / Task #", Location = new Point(16, 132), Size = new Size(200, 20) };
        _description = new TextBox
        {
            Location = new Point(16, 154),
            Size = new Size(376, 24)
        };

        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(216, 200),
            Size = new Size(80, 30)
        };
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(308, 200),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Controls.AddRange([lblDate, _datePicker, lblStart, _startPicker,
                           lblEnd, _endPicker, _totalLabel, lblDesc,
                           _description, btnSave, btnCancel]);

        UpdateTotal();
    }

    void UpdateTotal()
    {
        var start = TimeOnly.FromDateTime(_startPicker.Value);
        var end   = TimeOnly.FromDateTime(_endPicker.Value);
        var diff  = end - start;
        _totalLabel.Text = diff.TotalMinutes > 0
            ? $"Total: {diff.Hours}h {diff.Minutes}m"
            : "Total: —";
    }

    void OnSave(object? sender, EventArgs e)
    {
        var start = TimeOnly.FromDateTime(_startPicker.Value);
        var end   = TimeOnly.FromDateTime(_endPicker.Value);

        if (end <= start)
        {
            MessageBox.Show("End time must be after start time.", "Invalid Time",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entry = new WorkEntry
        {
            Date        = DateOnly.FromDateTime(_datePicker.Value),
            StartTime   = start,
            EndTime     = end,
            TotalHours  = Math.Round((end - start).TotalHours, 2),
            Description = _description.Text.Trim()
        };

        WorkEntryRepository.AddEntry(entry);
        DialogResult = DialogResult.OK;
        Close();
    }
}
