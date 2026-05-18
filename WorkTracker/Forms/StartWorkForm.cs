using WorkTracker.Data;

namespace WorkTracker.Forms;

class StartWorkForm : Form
{
    readonly DateTimePicker _timePicker;

    public StartWorkForm()
    {
        Text = "Start Work";
        Size = new Size(360, 170);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        var label = new Label
        {
            Text = "What time did you start working?",
            Location = new Point(16, 20),
            Size = new Size(320, 20),
            Font = new Font(Font.FontFamily, 10f)
        };

        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Now,
            Location = new Point(16, 52),
            Size = new Size(120, 28)
        };

        var btnStart = new Button
        {
            Text = "Start",
            Location = new Point(164, 96),
            Size = new Size(80, 30),
            DialogResult = DialogResult.None
        };
        btnStart.Click += OnStart;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(254, 96),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnStart;
        CancelButton = btnCancel;
        Controls.AddRange([label, _timePicker, btnStart, btnCancel]);
    }

    void OnStart(object? sender, EventArgs e)
    {
        var open = WorkEntryRepository.GetOpenSession();
        if (open is not null)
        {
            MessageBox.Show(
                "You already have an open work session.\nPlease finish it before starting a new one.",
                "Session Active", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var startTime = TimeOnly.FromDateTime(_timePicker.Value);
        WorkEntryRepository.CreatePendingEntry(DateOnly.FromDateTime(DateTime.Today), startTime);
        DialogResult = DialogResult.OK;
        Close();
    }
}
