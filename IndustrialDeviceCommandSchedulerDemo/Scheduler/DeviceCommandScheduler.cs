using System.Diagnostics;
using IndustrialDeviceCommandSchedulerDemo.Connections;

namespace IndustrialDeviceCommandSchedulerDemo.Scheduler;

public sealed class DeviceCommandScheduler : IDeviceCommandScheduler
{
    private readonly IDeviceConnection _connection;
    private readonly PriorityQueue<ScheduledDeviceCommand, CommandQueuePriority> _queue = new();
    private readonly SemaphoreSlim _queueSignal = new(0);
    private readonly object _queueLock = new();
    private readonly CancellationTokenSource _stopCts = new();
    private readonly Task _workerTask;
    private long _sequence;
    private SchedulerMetrics _metrics = new(0, 0, 0, 0, 0, 0, 0);

    public DeviceCommandScheduler(IDeviceConnection connection)
    {
        _connection = connection;
        _workerTask = Task.Run(() => WorkerLoopAsync(_stopCts.Token));
    }

    public int PendingCount
    {
        get
        {
            lock (_queueLock)
            {
                return _queue.Count;
            }
        }
    }

    public bool IsBusy { get; private set; }

    public IDeviceCommand? CurrentCommand { get; private set; }

    public SchedulerMetrics Metrics => _metrics;

    public event EventHandler<SchedulerEvent>? SchedulerEvent;

    public Task<DeviceCommandResult> ExecuteAsync(IDeviceCommand command, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DeviceCommandResult>(cancellationToken);
        }

        var dropped = DropCoalescedCommands(command);
        if (dropped > 0)
        {
            AddDropped(dropped);
        }

        var item = new ScheduledDeviceCommand
        {
            Command = command,
            CompletionSource = new TaskCompletionSource<DeviceCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            CancellationToken = cancellationToken,
            Sequence = Interlocked.Increment(ref _sequence),
            EnqueuedAt = DateTime.Now
        };

        lock (_queueLock)
        {
            _queue.Enqueue(item, new CommandQueuePriority((int)command.Priority, item.Sequence));
        }

        AddEnqueued();
        Publish(command, "Queued", "命令已进入优先级队列。");
        _queueSignal.Release();
        return item.CompletionSource.Task;
    }

    public async ValueTask DisposeAsync()
    {
        await _stopCts.CancelAsync().ConfigureAwait(false);
        _queueSignal.Release();

        try
        {
            await _workerTask.ConfigureAwait(false);
        }
        catch
        {
        }

        _queueSignal.Dispose();
        _stopCts.Dispose();
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _queueSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var item = Dequeue();
            if (item is null)
            {
                continue;
            }

            await ExecuteInternalAsync(item).ConfigureAwait(false);
        }
    }

    private ScheduledDeviceCommand? Dequeue()
    {
        lock (_queueLock)
        {
            return _queue.Count == 0 ? null : _queue.Dequeue();
        }
    }

    private async Task ExecuteInternalAsync(ScheduledDeviceCommand item)
    {
        if (item.CancellationToken.IsCancellationRequested)
        {
            item.CompletionSource.TrySetCanceled(item.CancellationToken);
            return;
        }

        CurrentCommand = item.Command;
        IsBusy = true;
        var queueTime = DateTime.Now - item.EnqueuedAt;

        try
        {
            Publish(item.Command, "Started", $"开始执行，排队 {queueTime.TotalMilliseconds:F0} ms。", queueTime);
            var result = await ExecuteWithRetryAsync(item.Command, queueTime, item.CancellationToken).ConfigureAwait(false);
            item.CompletionSource.TrySetResult(result);

            if (result.Success)
            {
                AddSucceeded();
                Publish(item.Command, "Completed", "命令执行成功。", result.QueueTime, result.ExecutionTime);
            }
            else
            {
                AddFailed(result.ErrorMessage?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true);
                Publish(item.Command, "Failed", result.ErrorMessage ?? "命令执行失败。", result.QueueTime, result.ExecutionTime);
            }
        }
        finally
        {
            AddCompleted();
            CurrentCommand = null;
            IsBusy = false;
        }
    }

    private async Task<DeviceCommandResult> ExecuteWithRetryAsync(IDeviceCommand command, TimeSpan queueTime, CancellationToken cancellationToken)
    {
        var maxRetry = command.CanRetry ? command.MaxRetryCount : 0;
        DeviceCommandResult? lastResult = null;

        for (var attempt = 0; attempt <= maxRetry; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (attempt > 0)
            {
                AddRetried();
                Publish(command, "Retry", $"第 {attempt} 次重试。");
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            var result = await ExecuteOnceAsync(command, queueTime, attempt, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                return result;
            }

            lastResult = result;
        }

        return lastResult!;
    }

    private async Task<DeviceCommandResult> ExecuteOnceAsync(IDeviceCommand command, TimeSpan queueTime, int retryCount, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(command.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var stopwatch = Stopwatch.StartNew();
        byte[]? request = null;

        try
        {
            request = command.BuildRequest();
            await _connection.SendAsync(request, linkedCts.Token).ConfigureAwait(false);
            var response = await _connection.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);

            stopwatch.Stop();
            if (!command.IsExpectedResponse(response))
            {
                return new DeviceCommandResult
                {
                    Success = false,
                    Request = request,
                    Response = response,
                    ErrorMessage = "Response mismatch",
                    RetryCount = retryCount,
                    QueueTime = queueTime,
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            return new DeviceCommandResult
            {
                Success = true,
                Request = request,
                Response = response,
                RetryCount = retryCount,
                QueueTime = queueTime,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new DeviceCommandResult
            {
                Success = false,
                Request = request,
                ErrorMessage = "Command timeout",
                RetryCount = retryCount,
                QueueTime = queueTime,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DeviceCommandResult
            {
                Success = false,
                Request = request,
                ErrorMessage = ex.Message,
                RetryCount = retryCount,
                QueueTime = queueTime,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    private int DropCoalescedCommands(IDeviceCommand command)
    {
        if (!command.CanDrop || string.IsNullOrWhiteSpace(command.CoalesceKey))
        {
            return 0;
        }

        lock (_queueLock)
        {
            if (_queue.Count == 0)
            {
                return 0;
            }

            var retained = new List<ScheduledDeviceCommand>();
            var dropped = 0;

            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                if (item.Command.CanDrop && item.Command.CoalesceKey == command.CoalesceKey)
                {
                    dropped++;
                    item.CompletionSource.TrySetResult(new DeviceCommandResult
                    {
                        Success = false,
                        ErrorMessage = "Dropped by coalesce policy"
                    });
                    continue;
                }

                retained.Add(item);
            }

            foreach (var item in retained)
            {
                _queue.Enqueue(item, new CommandQueuePriority((int)item.Command.Priority, item.Sequence));
            }

            return dropped;
        }
    }

    private void Publish(IDeviceCommand command, string stage, string message, TimeSpan? queueTime = null, TimeSpan? executionTime = null)
    {
        SchedulerEvent?.Invoke(
            this,
            new SchedulerEvent(
                DateTime.Now,
                _connection.DeviceId,
                command.CommandId,
                command.Name,
                command.Priority,
                stage,
                message,
                PendingCount,
                queueTime,
                executionTime));
    }

    private void AddEnqueued() => _metrics = _metrics with { Enqueued = _metrics.Enqueued + 1 };

    private void AddCompleted() => _metrics = _metrics with { Completed = _metrics.Completed + 1 };

    private void AddSucceeded() => _metrics = _metrics with { Succeeded = _metrics.Succeeded + 1 };

    private void AddFailed(bool timedOut)
    {
        _metrics = _metrics with
        {
            Failed = _metrics.Failed + 1,
            TimedOut = timedOut ? _metrics.TimedOut + 1 : _metrics.TimedOut
        };
    }

    private void AddDropped(int count) => _metrics = _metrics with { Dropped = _metrics.Dropped + count };

    private void AddRetried() => _metrics = _metrics with { Retried = _metrics.Retried + 1 };
}
