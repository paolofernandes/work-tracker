using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WorkTracker.Services;

static class WindowTitleScanner
{
    // Azure DevOps: #1234 or AB#1234
    static readonly Regex AdoPattern = new(@"(?:AB)?#(\d{3,6})", RegexOptions.Compiled);
    // Jira: PROJ-123
    static readonly Regex JiraPattern = new(@"\b([A-Z]{2,10}-\d+)\b", RegexOptions.Compiled);

    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// Scans open window titles for ADO/Jira ticket numbers.
    /// Returns the raw ticket string (e.g. "1234" or "PROJ-123"), or null if none found.
    /// </summary>
    public static string? Scan()
    {
        try
        {
            // Check foreground window first
            var fg = GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                var title = GetTitle(fg);
                var match = ExtractTicket(title);
                if (match is not null) return match;
            }

            // Enumerate all visible windows
            string? found = null;
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                var title = GetTitle(hWnd);
                var match = ExtractTicket(title);
                if (match is not null)
                {
                    found = match;
                    return false; // stop enumeration
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }
        catch
        {
            return null;
        }
    }

    static string GetTitle(IntPtr hWnd)
    {
        var sb = new StringBuilder(512);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    static string? ExtractTicket(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var adoMatch = AdoPattern.Match(title);
        if (adoMatch.Success) return adoMatch.Groups[1].Value;

        var jiraMatch = JiraPattern.Match(title);
        if (jiraMatch.Success) return jiraMatch.Groups[1].Value;

        return null;
    }
}
