using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace IndustrialDeviceCommandSchedulerDemo.Collections;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        var changed = false;
        foreach (var item in items)
        {
            Items.Add(item);
            changed = true;
        }

        if (changed)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public void TrimStart(int maxCount)
    {
        while (Items.Count > maxCount)
        {
            Items.RemoveAt(0);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
