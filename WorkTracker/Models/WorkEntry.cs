namespace WorkTracker.Models;

class WorkEntry
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public double? TotalHours { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsOpen => EndTime is null;
}
