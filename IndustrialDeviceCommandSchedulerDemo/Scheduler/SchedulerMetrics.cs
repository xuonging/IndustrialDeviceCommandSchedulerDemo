namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public sealed record SchedulerMetrics(
    long Enqueued,
    long Completed,
    long Succeeded,
    long Failed,
    long Dropped,
    long Retried,
    long TimedOut);
