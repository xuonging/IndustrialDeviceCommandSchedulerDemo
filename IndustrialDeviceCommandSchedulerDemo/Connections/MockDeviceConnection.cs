using System.Text;

namespace IndustrialDeviceCommandSchedulerDemo.Connections;

public sealed class MockDeviceConnection : IDeviceConnection
{
    private readonly Random _random = new();
    private byte[]? _lastRequest;
    private int _temperatureSeed = 350;

    public MockDeviceConnection(string deviceId)
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }

    public async Task SendAsync(byte[] request, CancellationToken cancellationToken)
    {
        _lastRequest = request.ToArray();
        await Task.Delay(_random.Next(15, 70), cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_random.Next(30, 140), cancellationToken).ConfigureAwait(false);

        var requestText = Encoding.ASCII.GetString(_lastRequest ?? Array.Empty<byte>()).Trim();
        var response = requestText switch
        {
            "READ TEMP" => $"TEMP={Interlocked.Increment(ref _temperatureSeed) / 10.0:F1}\r\n",
            "READ PRESSURE" => $"PRESSURE={1 + _random.NextDouble() * 0.4:F2}\r\n",
            "START" => "START OK\r\n",
            "STOP" => "STOP OK\r\n",
            "ESTOP" => "ESTOP OK\r\n",
            "SLOW" => CreateSlowResponse(cancellationToken).GetAwaiter().GetResult(),
            _ => "ERR UNKNOWN\r\n"
        };

        return Encoding.ASCII.GetBytes(response);
    }

    private static async Task<string> CreateSlowResponse(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        return "SLOW OK\r\n";
    }
}
