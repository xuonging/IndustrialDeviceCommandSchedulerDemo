using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class SlowCommand : TextDeviceCommand
{
    public SlowCommand()
        : base("超时测试")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.Normal;

    public override TimeSpan Timeout => TimeSpan.FromMilliseconds(650);

    public override int MaxRetryCount => 1;

    public override bool CanRetry => true;

    public override bool CanDrop => false;

    public override string? CoalesceKey => null;

    protected override string RequestText => "SLOW";

    protected override string ExpectedPrefix => "SLOW OK";
}
