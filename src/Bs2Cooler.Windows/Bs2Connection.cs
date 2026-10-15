using Bs2Cooler.Core;

namespace Bs2Cooler.Windows;

public sealed class Bs2Connection : IAsyncDisposable
{
    private readonly FileStream _stream;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _commands = new(1, 1);
    private readonly object _pendingLock = new();
    private readonly Task _reader;
    private Pending? _pending;
    private bool _disposed;
    private sealed record Pending(byte Command, TaskCompletionSource<Bs2Frame> Completion);

    public event Action<CoolerStatus>? StatusReceived;
    public event Action<Exception>? Disconnected;
    public CoolerStatus? LatestStatus { get; private set; }
    public Bs2Device Device { get; }

    public Bs2Connection(Bs2Device device)
    {
        Device = device;
        _stream = NativeHid.Open(device);
        _reader = ReadLoopAsync();
    }

    public async Task<Bs2Frame> SendAsync(byte[] packet, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (packet.Length != Bs2Protocol.OutputLength || packet[0] != 0 || packet[1] != 0x5A || packet[2] != 0xA5)
            throw new ArgumentException("잘못된 USB 명령입니다.", nameof(packet));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _commands.WaitAsync(linked.Token).ConfigureAwait(false);
        var pending = new Pending(packet[3], new(TaskCreationOptions.RunContinuationsAsynchronously));
        try
        {
            lock (_pendingLock) _pending = pending;
            await _stream.WriteAsync(packet, linked.Token).ConfigureAwait(false);
            return await pending.Completion.Task.WaitAsync(TimeSpan.FromMilliseconds(1500), linked.Token).ConfigureAwait(false);
        }
        catch (TimeoutException) { throw new TimeoutException($"BS2 Pro 응답 시간 초과 (명령 0x{packet[3]:X2}). USB 연결을 확인해 주세요."); }
        finally
        {
            lock (_pendingLock) { if (ReferenceEquals(_pending, pending)) _pending = null; }
            _commands.Release();
        }
    }

    public async Task<FanSpeed> ReadRpmAsync(CancellationToken token = default) =>
        Bs2Protocol.ParseRpm(await SendAsync(Bs2Protocol.Query(Bs2Command.Rpm), token).ConfigureAwait(false));

    public async Task SetManualAsync(int gear, int rpm, CancellationToken token = default)
    {
        // Require a recent power reading before a write; never infer that a 4000 RPM supply is present.
        var status = LatestStatus;
        if (status == null || DateTimeOffset.Now - status.ReceivedAt > TimeSpan.FromSeconds(6))
            throw new InvalidOperationException("장치의 전원 상태를 확인 중입니다. 잠시 후 다시 적용해 주세요.");
        if (status.IsShutdown) throw new InvalidOperationException("쿨러 전원을 켠 후 다시 적용해 주세요.");
        Bs2Protocol.ValidateRpm(rpm, status.MaximumRpm);
        if (status.IsSmart)
            Bs2Protocol.RequireSuccess(await SendAsync(Bs2Protocol.ExitSmart(), token).ConfigureAwait(false), Bs2Command.ExitSmart);
        Bs2Protocol.RequireSuccess(await SendAsync(Bs2Protocol.SetGear(gear, rpm, status.MaximumRpm), token).ConfigureAwait(false), Bs2Command.SetGear);
    }

    public async Task SetRgbAsync(bool enabled, CancellationToken token = default) =>
        Bs2Protocol.RequireSuccess(await SendAsync(Bs2Protocol.Rgb(enabled), token).ConfigureAwait(false), Bs2Command.SetRgb);

    public async Task SetGearLightAsync(bool enabled, CancellationToken token = default) =>
        Bs2Protocol.RequireSuccess(await SendAsync(Bs2Protocol.GearLight(enabled), token).ConfigureAwait(false), Bs2Command.SetGearLight);

    private async Task ReadLoopAsync()
    {
        byte[] buffer = new byte[Device.InputLength];
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                int length = await _stream.ReadAsync(buffer, _lifetime.Token).ConfigureAwait(false);
                if (length == 0) throw new IOException("USB 연결이 종료되었습니다.");
                if (!Bs2Protocol.TryDecode(buffer.AsSpan(0, length), out var frame) || frame == null) continue;
                if (frame.Command == (byte)Bs2Command.Status && frame.Payload.Length >= 9)
                {
                    LatestStatus = Bs2Protocol.ParseStatus(frame);
                    StatusReceived?.Invoke(LatestStatus);
                }
                lock (_pendingLock)
                {
                    if (_pending?.Command == frame.Command) _pending.Completion.TrySetResult(frame);
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException && _lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            lock (_pendingLock) _pending?.Completion.TrySetException(ex);
            _lifetime.Cancel();
            Disconnected?.Invoke(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _lifetime.CancelAsync().ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
        await _reader.ConfigureAwait(false);
        // Semaphore and CTS remain valid for a command that is unwinding cancellation.
    }
}
