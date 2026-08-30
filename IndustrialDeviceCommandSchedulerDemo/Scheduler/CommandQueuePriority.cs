namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

internal readonly record struct CommandQueuePriority(int Priority, long Sequence)
    : IComparable<CommandQueuePriority>
{
    public int CompareTo(CommandQueuePriority other)
    {
        var priority = Priority.CompareTo(other.Priority);
        return priority != 0 ? priority : Sequence.CompareTo(other.Sequence);
    }
}
