using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class ReadPressureCommand : TextDeviceCommand
{
    public ReadPressureCommand()
        : base("读取压力")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.Background;

    public override TimeSpan Timeout => TimeSpan.FromSeconds(1);

    public override int MaxRetryCount => 1;

    public override bool CanRetry => true;

    public override bool CanDrop => true;

    public override string? CoalesceKey => "READ_PRESSURE";

    protected override string RequestText => "READ PRESSURE";

    protected override string ExpectedPrefix => "PRESSURE=";
}
