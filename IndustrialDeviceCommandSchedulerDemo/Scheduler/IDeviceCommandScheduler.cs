namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public interface IDeviceCommandScheduler : IAsyncDisposable
{
    int PendingCount { get; }

    bool IsBusy { get; }

    IDeviceCommand? CurrentCommand { get; }

    SchedulerMetrics Metrics { get; }

    event EventHandler<SchedulerEvent>? SchedulerEvent;

    Task<DeviceCommandResult> ExecuteAsync(IDeviceCommand command, CancellationToken cancellationToken = default);
}
