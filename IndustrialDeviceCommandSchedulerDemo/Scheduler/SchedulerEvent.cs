namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public sealed record SchedulerEvent(
    DateTime Timestamp,
    string DeviceId,
    string CommandId,
    string CommandName,
    DeviceCommandPriority Priority,
    string Stage,
    string Message,
    int PendingCount,
    TimeSpan? QueueTime = null,
    TimeSpan? ExecutionTime = null);
