namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

internal sealed record ScheduledDeviceCommand
{
    public required IDeviceCommand Command { get; init; }

    public required TaskCompletionSource<DeviceCommandResult> CompletionSource { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public required long Sequence { get; init; }

    public required DateTime EnqueuedAt { get; init; }
}
