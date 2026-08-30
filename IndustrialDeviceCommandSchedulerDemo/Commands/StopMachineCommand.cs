using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class StopMachineCommand : TextDeviceCommand
{
    public StopMachineCommand()
        : base("人工停止")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.ManualControl;

    public override TimeSpan Timeout => TimeSpan.FromSeconds(2);

    public override int MaxRetryCount => 0;

    public override bool CanRetry => false;

    public override bool CanDrop => false;

    public override string? CoalesceKey => null;

    protected override string RequestText => "STOP";

    protected override string ExpectedPrefix => "STOP OK";
}
