namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public sealed record DeviceCommandResult
{
    public bool Success { get; init; }

    public byte[]? Request { get; init; }

    public byte[]? Response { get; init; }

    public string? ErrorMessage { get; init; }

    public int RetryCount { get; init; }

    public TimeSpan QueueTime { get; init; }

    public TimeSpan ExecutionTime { get; init; }

    public TimeSpan Elapsed => QueueTime + ExecutionTime;
}
