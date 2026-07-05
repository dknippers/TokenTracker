namespace TokenTrackerWidget.Models;

public sealed record DayUsageSnapshot(
    string DayKey,
    long Input,
    long Output,
    long Reasoning,
    long CacheRead,
    long CacheWrite,
    double Cost,
    IReadOnlyList<ModelBreakdown> Models,
    DateTimeOffset TakenAt)
{
    public static DayUsageSnapshot Empty(string? dayKey = null)
        => new(
            DayKey: dayKey ?? DateTimeOffset.Now.ToString("yyyy-MM-dd"),
            Input: 0, Output: 0, Reasoning: 0, CacheRead: 0, CacheWrite: 0,
            Cost: 0,
            Models: Array.Empty<ModelBreakdown>(),
            TakenAt: DateTimeOffset.Now);
}