using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class EmergencyStopCommand : TextDeviceCommand
{
    public EmergencyStopCommand()
        : base("紧急停止")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.Emergency;

    public override TimeSpan Timeout => TimeSpan.FromMilliseconds(500);

    public override int MaxRetryCount => 1;

    public override bool CanRetry => true;

    public override bool CanDrop => false;

    public override string? CoalesceKey => "ESTOP";

    protected override string RequestText => "ESTOP";

    protected override string ExpectedPrefix => "ESTOP OK";
}
