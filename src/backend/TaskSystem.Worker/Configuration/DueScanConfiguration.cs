namespace TaskSystem.Worker.Configuration;

/// <summary>
/// Configuration constants for DueScan worker.
/// </summary>
public static class DueScanConfiguration
{
    public const string SectionName = "DueScan";
    public const string IntervalSecondsKey = "DueScan:IntervalSeconds";
    public const string BatchSizeKey = "DueScan:BatchSize";
    
    // Defaults
    public const int DefaultIntervalSeconds = 15;
    public const int DefaultBatchSize = 50;
    public const int MinIntervalSeconds = 5;
    public const int MaxBatchSize = 1000;
}

