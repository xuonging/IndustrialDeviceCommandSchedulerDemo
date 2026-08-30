using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class StartMachineCommand : TextDeviceCommand
{
    public StartMachineCommand()
        : base("人工启动")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.ManualControl;

    public override TimeSpan Timeout => TimeSpan.FromSeconds(2);

    public override int MaxRetryCount => 0;

    public override bool CanRetry => false;

    public override bool CanDrop => false;

    public override string? CoalesceKey => null;

    protected override string RequestText => "START";

    protected override string ExpectedPrefix => "START OK";
}
