using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WorkTracker.Services;

/// <summary>
/// Detects active screen sharing for Zoom and Microsoft Teams.
/// Google Meet and Webex are not detected in v1.
/// </summary>
static class ScreenShareDetector
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static bool IsSharing()
    {
        try
        {
            return IsZoomSharing() || IsTeamsSharing();
        }
        catch
        {
            return false; // fail open — never block the prompt on detection error
        }
    }

    static bool IsZoomSharing()
    {
        // Zoom renders a ZPToolbar window only during an active screen share
        return FindWindow("ZPToolbar", null) != IntPtr.Zero;
    }

    static bool IsTeamsSharing()
    {
        var teamsProcessIds = Process.GetProcessesByName("ms-teams")
            .Concat(Process.GetProcessesByName("Teams"))
            .Select(p => (uint)p.Id)
            .ToHashSet();

        if (teamsProcessIds.Count == 0) return false;

        bool sharing = false;
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (!teamsProcessIds.Contains(pid)) return true;

            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString().ToLowerInvariant();

            // PT-BR and EN indicators for Teams sharing state
            if (title.Contains("sharing") || title.Contains("apresentando") ||
                title.Contains("presenting") || title.Contains("compartilhando"))
            {
                sharing = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return sharing;
    }
}
