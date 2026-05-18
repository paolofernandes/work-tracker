using Microsoft.Win32;

namespace WorkTracker.Services;

static class AutoStartService
{
    const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "WorkTracker";

    public static void Enable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        key?.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        var value = key?.GetValue(ValueName) as string;
        return value is not null &&
               value.Trim('"') == Application.ExecutablePath;
    }
}
