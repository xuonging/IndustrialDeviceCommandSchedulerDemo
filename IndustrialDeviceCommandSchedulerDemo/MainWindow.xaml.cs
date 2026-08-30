using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using IndustrialDeviceCommandSchedulerDemo.Collections;
using IndustrialDeviceCommandSchedulerDemo.Commands;
using IndustrialDeviceCommandSchedulerDemo.Connections;
using IndustrialDeviceCommandSchedulerDemo.Scheduler;

namespace IndustrialDeviceCommandSchedulerDemo;

public partial class MainWindow : Window
{
    private const int MaxLogCount = 180;
    private readonly DeviceCommandScheduler _scheduler;
    private readonly DispatcherTimer _statusTimer = new();
    private CancellationTokenSource? _backgroundCts;
    private CancellationTokenSource? _pageCts = new();
    private Task? _backgroundTask;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _scheduler = new DeviceCommandScheduler(new MockDeviceConnection("PLC-01"));
        _scheduler.SchedulerEvent += OnSchedulerEvent;

        _statusTimer.Interval = TimeSpan.FromMilliseconds(150);
        _statusTimer.Tick += (_, _) => RefreshMetrics();
        _statusTimer.Start();

        TxtUiThread.Text = $"UI Thread #{Environment.CurrentManagedThreadId}";
        AddLog("Demo 已启动。所有命令都必须经过 IDeviceCommandScheduler。");
    }

    public BulkObservableCollection<string> LogItems { get; } = new();

    private void BtnStartBackground_Click(object sender, RoutedEventArgs e)
    {
        if (_backgroundTask is { IsCompleted: false })
        {
            return;
        }

        _backgroundCts = new CancellationTokenSource();
        _backgroundTask = Task.Run(() => BackgroundCollectLoopAsync(_backgroundCts.Token));
        AddLog("后台采集已启动：每 120ms 提交温度和压力读取，优先级 Background。");
    }

    private async void BtnStopBackground_Click(object sender, RoutedEventArgs e)
    {
        await StopBackgroundAsync();
    }

    private async void BtnReadTemperature_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new ReadTemperatureCommand());
    }

    private async void BtnReadPressure_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new ReadPressureCommand());
    }

    private async void BtnStartMachine_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new StartMachineCommand());
    }

    private async void BtnStopMachine_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new StopMachineCommand());
    }

    private async void BtnEmergencyStop_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new EmergencyStopCommand());
    }

    private async void BtnSlow_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteFromUiAsync(new SlowCommand());
    }

    private void BtnCancelPageRequests_Click(object sender, RoutedEventArgs e)
    {
        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = new CancellationTokenSource();
        AddLog("页面请求已取消：排队中且携带该 token 的命令会跳过。");
    }

    private async Task ExecuteFromUiAsync(IDeviceCommand command)
    {
        try
        {
            var result = await _scheduler.ExecuteAsync(command, _pageCts?.Token ?? CancellationToken.None);
            ShowResult(command, result);
        }
        catch (OperationCanceledException)
        {
            TxtLatestResult.Text = $"{command.Name} 已取消。";
        }
    }

    private async Task BackgroundCollectLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(120));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            _ = _scheduler.ExecuteAsync(new ReadTemperatureCommand(), cancellationToken);
            _ = _scheduler.ExecuteAsync(new ReadPressureCommand(), cancellationToken);
        }
    }

    private void OnSchedulerEvent(object? sender, SchedulerEvent e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var queue = e.QueueTime is null ? string.Empty : $" Queue={e.QueueTime.Value.TotalMilliseconds:F0}ms";
            var exec = e.ExecutionTime is null ? string.Empty : $" Exec={e.ExecutionTime.Value.TotalMilliseconds:F0}ms";
            AddLog($"[{e.Timestamp:HH:mm:ss.fff}] [{e.Priority}] {e.CommandName,-8} {e.Stage,-9} Pending={e.PendingCount}{queue}{exec} {e.Message}");
            RefreshMetrics();
        });
    }

    private void RefreshMetrics()
    {
        var metrics = _scheduler.Metrics;
        TxtCurrentCommand.Text = _scheduler.CurrentCommand?.Name ?? "空闲";
        TxtBusy.Text = _scheduler.IsBusy ? "Busy" : "Idle";
        TxtBusy.Foreground = _scheduler.IsBusy ? Brushes.DarkOrange : Brushes.ForestGreen;
        TxtPending.Text = _scheduler.PendingCount.ToString("N0");
        TxtSuccessFailed.Text = $"{metrics.Succeeded:N0} / {metrics.Failed:N0}";
        TxtDropRetryTimeout.Text = $"{metrics.Dropped:N0} / {metrics.Retried:N0} / {metrics.TimedOut:N0}";
        TxtTotal.Text = $"Enqueued {metrics.Enqueued:N0}, Completed {metrics.Completed:N0}";
    }

    private void ShowResult(IDeviceCommand command, DeviceCommandResult result)
    {
        var response = result.Response is null ? "<null>" : Encoding.ASCII.GetString(result.Response).Trim();
        TxtLatestResult.Text =
            $"{command.Name}: {(result.Success ? "成功" : "失败")}\n" +
            $"响应: {response}\n" +
            $"排队: {result.QueueTime.TotalMilliseconds:F0} ms, 执行: {result.ExecutionTime.TotalMilliseconds:F0} ms, 重试: {result.RetryCount}\n" +
            $"错误: {result.ErrorMessage ?? "-"}";
    }

    private void AddLog(string message)
    {
        LogItems.Add(message);
        LogItems.TrimStart(MaxLogCount);
        if (LogListBox.Items.Count > 0)
        {
            LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        }
    }

    private async Task StopBackgroundAsync()
    {
        if (_backgroundCts is null)
        {
            return;
        }

        await _backgroundCts.CancelAsync();
        if (_backgroundTask is not null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _backgroundCts.Dispose();
        _backgroundCts = null;
        _backgroundTask = null;
        AddLog("后台采集已停止。");
    }

    protected override async void OnClosed(EventArgs e)
    {
        await StopBackgroundAsync();
        _statusTimer.Stop();
        _pageCts?.Cancel();
        _pageCts?.Dispose();
        await _scheduler.DisposeAsync();
        base.OnClosed(e);
    }
}
