namespace WorkTracker.Models;

class AppSettings
{
    public TimeOnly Prompt1Start { get; set; } = new TimeOnly(10, 0);
    public TimeOnly Prompt1End { get; set; } = new TimeOnly(11, 0);
    public TimeOnly Prompt2Start { get; set; } = new TimeOnly(15, 0);
    public TimeOnly Prompt2End { get; set; } = new TimeOnly(16, 0);
    public string ArtiaActivityId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = "paolo.fernandes@lyncas.net";

    public static AppSettings Defaults => new();
}
