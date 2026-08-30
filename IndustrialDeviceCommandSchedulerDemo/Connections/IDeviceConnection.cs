namespace IndustrialDeviceCommandSchedulerDemo.Connections;

public interface IDeviceConnection
{
    string DeviceId { get; }

    Task SendAsync(byte[] request, CancellationToken cancellationToken);

    Task<byte[]> ReceiveAsync(CancellationToken cancellationToken);
}
