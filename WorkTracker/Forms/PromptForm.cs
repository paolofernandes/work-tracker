using WorkTracker.App;
using WorkTracker.Data;
using WorkTracker.Services;

namespace WorkTracker.Forms;

class PromptForm : Form
{
    readonly DateTimePicker _timePicker;
    readonly TextBox _description;
    readonly ComboBox _delayCombo;
    readonly PromptScheduler _scheduler;
    bool _delayHandled;

    public PromptForm(PromptScheduler scheduler)
    {
        _scheduler = scheduler;

        Text = "Work Tracker — What are you working on?";
        Size = new Size(430, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        var lblQuestion = new Label
        {
            Text = "What are you working on?",
            Location = new Point(16, 16),
            Size = new Size(390, 22),
            Font = new Font(Font.FontFamily, 11f, FontStyle.Bold)
        };

        var lblTime = new Label
        {
            Text = "Start time:",
            Location = new Point(16, 52),
            Size = new Size(70, 20)
        };

        _timePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Now,
            Location = new Point(90, 48),
            Size = new Size(110, 28)
        };

        var lblDesc = new Label
        {
            Text = "Description / Task #",
            Location = new Point(16, 86),
            Size = new Size(200, 20)
        };

        _description = new TextBox
        {
            Location = new Point(16, 108),
            Size = new Size(390, 24),
            Text = BuildSuggestion(WindowTitleScanner.Scan())
        };

        var btnConfirm = new Button
        {
            Text = "Confirm",
            Location = new Point(16, 152),
            Size = new Size(90, 30)
        };
        btnConfirm.Click += OnConfirm;

        _delayCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(240, 152),
            Size = new Size(90, 28)
        };
        _delayCombo.Items.AddRange(["15 min", "30 min", "60 min"]);
        _delayCombo.SelectedIndex = 0;

        var btnDelay = new Button
        {
            Text = "Delay",
            Location = new Point(340, 152),
            Size = new Size(66, 30)
        };
        btnDelay.Click += OnDelay;

        AcceptButton = btnConfirm;
        Controls.AddRange([lblQuestion, lblTime, _timePicker, lblDesc, _description,
                           btnConfirm, _delayCombo, btnDelay]);

        FormClosing += OnFormClosing;
    }

    void OnConfirm(object? sender, EventArgs e)
    {
        var startTime = TimeOnly.FromDateTime(_timePicker.Value);
        WorkEntryRepository.CreatePendingEntry(DateOnly.FromDateTime(DateTime.Today), startTime);
        _delayHandled = true;
        DialogResult = DialogResult.OK;
        Close();
    }

    void OnDelay(object? sender, EventArgs e)
    {
        int minutes = _delayCombo.SelectedIndex switch
        {
            1 => 30,
            2 => 60,
            _ => 15
        };
        _scheduler.Delay(minutes);
        _delayHandled = true;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_delayHandled)
            _scheduler.Delay(15);
    }

    static string BuildSuggestion(string? ticket) =>
        ticket is null ? string.Empty : $"Are you still working on task #{ticket}?";
}
