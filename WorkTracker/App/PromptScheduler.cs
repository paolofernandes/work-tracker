using WorkTracker.Data;
using WorkTracker.Services;

namespace WorkTracker.App;

class PromptScheduler : IDisposable
{
    public event EventHandler? PromptDue;

    readonly System.Threading.Timer _timer;
    DateTime _nextFire1;
    DateTime _nextFire2;
    DateTime _delayUntil = DateTime.MinValue;
    bool _window1Fired;
    bool _window2Fired;
    Models.AppSettings _settings;
    readonly Control _invokeTarget;

    public PromptScheduler(Control invokeTarget)
    {
        _invokeTarget = invokeTarget;
        _settings = SettingsRepository.GetAll();
        CalculateFireTimes(DateTime.Now);
        _timer = new System.Threading.Timer(OnTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public void Delay(int minutes)
    {
        _delayUntil = DateTime.Now.AddMinutes(minutes);
    }

    public void Restart(Models.AppSettings settings)
    {
        _settings = settings;
        _window1Fired = false;
        _window2Fired = false;
        CalculateFireTimes(DateTime.Now);
    }

    void CalculateFireTimes(DateTime basis)
    {
        _nextFire1 = RandomTimeInWindow(basis.Date, _settings.Prompt1Start, _settings.Prompt1End);
        _nextFire2 = RandomTimeInWindow(basis.Date, _settings.Prompt2Start, _settings.Prompt2End);

        // If today's fire time already passed, schedule for tomorrow
        if (_nextFire1 <= DateTime.Now) { _nextFire1 = _nextFire1.AddDays(1); _window1Fired = false; }
        if (_nextFire2 <= DateTime.Now) { _nextFire2 = _nextFire2.AddDays(1); _window2Fired = false; }
    }

    void OnTick(object? _)
    {
        var now = DateTime.Now;

        // Skip weekends
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;

        // Respect delay
        if (now < _delayUntil) return;

        bool shouldFire = false;

        if (!_window1Fired && now >= _nextFire1)
        {
            _window1Fired = true;
            shouldFire = true;
            _nextFire1 = RandomTimeInWindow(now.Date.AddDays(1), _settings.Prompt1Start, _settings.Prompt1End);
        }
        else if (!_window2Fired && now >= _nextFire2)
        {
            _window2Fired = true;
            shouldFire = true;
            _nextFire2 = RandomTimeInWindow(now.Date.AddDays(1), _settings.Prompt2Start, _settings.Prompt2End);
        }

        if (!shouldFire) return;

        // Hold if screen is being shared — retry on next tick
        if (ScreenShareDetector.IsSharing())
        {
            // Roll back the fired flag so we retry
            if (_window1Fired && now >= _nextFire1.AddDays(-1))
                _window1Fired = false;
            else
                _window2Fired = false;
            return;
        }

        RaisePromptDue();
    }

    void RaisePromptDue()
    {
        try
        {
            if (_invokeTarget.IsHandleCreated)
                _invokeTarget.Invoke(() => PromptDue?.Invoke(this, EventArgs.Empty));
        }
        catch { /* form may be disposed during shutdown */ }
    }

    static DateTime RandomTimeInWindow(DateTime date, TimeOnly start, TimeOnly end)
    {
        var startMinutes = (int)start.ToTimeSpan().TotalMinutes;
        var endMinutes   = (int)end.ToTimeSpan().TotalMinutes;
        var offset = Random.Shared.Next(startMinutes, Math.Max(startMinutes + 1, endMinutes));
        return date.Date.AddMinutes(offset);
    }

    public void Dispose() => _timer.Dispose();
}
