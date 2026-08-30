namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public interface IDeviceCommand
{
    string CommandId { get; }

    string Name { get; }

    DeviceCommandPriority Priority { get; }

    TimeSpan Timeout { get; }

    int MaxRetryCount { get; }

    bool CanRetry { get; }

    bool CanDrop { get; }

    string? CoalesceKey { get; }

    byte[] BuildRequest();

    bool IsExpectedResponse(byte[] response);
}
