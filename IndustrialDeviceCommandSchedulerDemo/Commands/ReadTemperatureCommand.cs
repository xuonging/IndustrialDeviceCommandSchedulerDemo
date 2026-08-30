using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo.Commands;

public sealed class ReadTemperatureCommand : TextDeviceCommand
{
    public ReadTemperatureCommand()
        : base("读取温度")
    {
    }

    public override DeviceCommandPriority Priority => DeviceCommandPriority.Background;

    public override TimeSpan Timeout => TimeSpan.FromSeconds(1);

    public override int MaxRetryCount => 2;

    public override bool CanRetry => true;

    public override bool CanDrop => true;

    public override string? CoalesceKey => "READ_TEMP";

    protected override string RequestText => "READ TEMP";

    protected override string ExpectedPrefix => "TEMP=";
}
