using WorkTracker.App;
using WorkTracker.Data;
using WorkTracker.Models;

namespace WorkTracker.Forms;

class SettingsForm : Form
{
    readonly DateTimePicker _w1Start, _w1End, _w2Start, _w2End;
    readonly TextBox _activityId, _email;
    readonly PromptScheduler _scheduler;

    public SettingsForm(PromptScheduler scheduler)
    {
        _scheduler = scheduler;

        Text = "Settings";
        Size = new Size(400, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // --- Prompt Windows section ---
        var grpPrompt = new GroupBox
        {
            Text = "Prompt Windows",
            Location = new Point(12, 12),
            Size = new Size(360, 110)
        };

        grpPrompt.Controls.Add(new Label { Text = "Window 1:", Location = new Point(10, 26), Size = new Size(70, 20) });
        _w1Start = MakeTimePicker(new Point(82, 22), grpPrompt);
        grpPrompt.Controls.Add(new Label { Text = "→", Location = new Point(200, 26), Size = new Size(20, 20) });
        _w1End = MakeTimePicker(new Point(222, 22), grpPrompt);

        grpPrompt.Controls.Add(new Label { Text = "Window 2:", Location = new Point(10, 64), Size = new Size(70, 20) });
        _w2Start = MakeTimePicker(new Point(82, 60), grpPrompt);
        grpPrompt.Controls.Add(new Label { Text = "→", Location = new Point(200, 64), Size = new Size(20, 20) });
        _w2End = MakeTimePicker(new Point(222, 60), grpPrompt);

        // --- Artia Export section ---
        var grpArtia = new GroupBox
        {
            Text = "Artia Export",
            Location = new Point(12, 132),
            Size = new Size(360, 120)
        };

        grpArtia.Controls.Add(new Label { Text = "ID da Atividade:", Location = new Point(10, 26), Size = new Size(110, 20) });
        _activityId = new TextBox { Location = new Point(124, 22), Size = new Size(220, 24) };
        grpArtia.Controls.Add(_activityId);

        grpArtia.Controls.Add(new Label { Text = "E-mail:", Location = new Point(10, 62), Size = new Size(110, 20) });
        _email = new TextBox { Location = new Point(124, 58), Size = new Size(220, 24) };
        grpArtia.Controls.Add(_email);

        // --- Buttons ---
        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(192, 268),
            Size = new Size(90, 30)
        };
        btnSave.Click += OnSave;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(292, 268),
            Size = new Size(90, 30),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Controls.AddRange([grpPrompt, grpArtia, btnSave, btnCancel]);

        LoadSettings();
    }

    void LoadSettings()
    {
        var s = SettingsRepository.GetAll();
        _w1Start.Value = DateTime.Today + s.Prompt1Start.ToTimeSpan();
        _w1End.Value   = DateTime.Today + s.Prompt1End.ToTimeSpan();
        _w2Start.Value = DateTime.Today + s.Prompt2Start.ToTimeSpan();
        _w2End.Value   = DateTime.Today + s.Prompt2End.ToTimeSpan();
        _activityId.Text = s.ArtiaActivityId;
        _email.Text      = s.UserEmail;
    }

    void OnSave(object? sender, EventArgs e)
    {
        var settings = new AppSettings
        {
            Prompt1Start    = TimeOnly.FromDateTime(_w1Start.Value),
            Prompt1End      = TimeOnly.FromDateTime(_w1End.Value),
            Prompt2Start    = TimeOnly.FromDateTime(_w2Start.Value),
            Prompt2End      = TimeOnly.FromDateTime(_w2End.Value),
            ArtiaActivityId = _activityId.Text.Trim(),
            UserEmail       = _email.Text.Trim()
        };

        SettingsRepository.SaveAll(settings);
        _scheduler.Restart(settings);
        DialogResult = DialogResult.OK;
        Close();
    }

    static DateTimePicker MakeTimePicker(Point location, Control parent)
    {
        var p = new DateTimePicker
        {
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Location = location,
            Size = new Size(110, 28)
        };
        parent.Controls.Add(p);
        return p;
    }
}
