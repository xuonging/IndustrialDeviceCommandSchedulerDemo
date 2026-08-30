using System.Text;
using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public abstract class TextDeviceCommand : IDeviceCommand
{
    protected TextDeviceCommand(string name)
    {
        Name = name;
    }

    public string CommandId { get; } = Guid.NewGuid().ToString("N");

    public string Name { get; }

    public abstract DeviceCommandPriority Priority { get; }

    public abstract TimeSpan Timeout { get; }

    public abstract int MaxRetryCount { get; }

    public abstract bool CanRetry { get; }

    public abstract bool CanDrop { get; }

    public abstract string? CoalesceKey { get; }

    protected abstract string RequestText { get; }

    protected abstract string ExpectedPrefix { get; }

    public byte[] BuildRequest()
    {
        return Encoding.ASCII.GetBytes(RequestText + "\r\n");
    }

    public bool IsExpectedResponse(byte[] response)
    {
        var text = Encoding.ASCII.GetString(response);
        return text.StartsWith(ExpectedPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
