using System.Collections.Concurrent;
using System.Collections.Frozen;
using Bluetuith.Shim.DataTypes;
using Bluetuith.Shim.Operations;
using Windows.Devices.Bluetooth;
using static Bluetuith.Shim.DataTypes.IEvent;
using Lock = DotNext.Threading.Lock;

namespace Bluetuith.Shim.Stack.Microsoft;

internal static partial class DiscoveryWatcher
{
    private partial record class DiscoveryClient(
        OperationToken Token,
        DevicesWatcher.Subscriber Subscriber
    ) : IDisposable
    {
        public void Dispose()
        {
            Subscriber?.Dispose();
            Token.Release();
        }
    }

    private static readonly ConcurrentDictionary<Guid, DiscoveryClient> clients = [];
    private static DevicesWatcher monitor = new(
        BluetoothDevice.GetDeviceSelectorFromPairingState(false)
    );
    private static readonly Lock @lock = Lock.Semaphore(new(1, 1));

    public static bool IsStarted(OperationToken token)
    {
        return clients.ContainsKey(token.ClientId);
    }

    internal static ErrorData Start(OperationToken token)
    {
        if (!@lock.TryAcquire(out var holder))
            return Errors.ErrorOperationInProgress;

        using var sem = holder;

        var adapterEvent = new AdapterEvent();
        var error = adapterEvent.MergeRadioEventData();
        if (error != Errors.ErrorNone)
            return error;

        if (!token.HasClientId)
            return Errors.ErrorUnexpected.AddMetadata(
                "exception",
                "Token does not contain a client ID"
            );

        if (clients.ContainsKey(token.ClientId))
            return Errors.ErrorDeviceDiscovery.AddMetadata(
                "exception",
                "Device discovery is already running"
            );

        clients.TryAdd(token.ClientId, new DiscoveryClient(token, Subscriber(monitor, token)));

        try
        {
            if (!monitor.Start())
                throw new Exception("The device discovery service is being reset");

            Output.ClientEvent(
                adapterEvent with
                {
                    OptionDiscovering = true,
                    Action = EventAction.Updated,
                },
                token
            );
        }
        catch (Exception e)
        {
            StopInternal(out var _, token, true, true, false);
            return Errors.ErrorDeviceDiscovery.AddMetadata("exception", e.Message);
        }

        foreach (var device in monitor.CurrentDevices)
        {
            if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                Output.ClientEvent(device with { Action = EventAction.Added }, token);
        }

        OperationManager.SetOperationProperties(token, true, true);

        return Errors.ErrorNone;
    }

    internal static ErrorData Stop(OperationToken token)
    {
        if (!StopInternal(out var error, token, false))
            return error;

        if (clients.IsEmpty)
            monitor.Stop();

        return Errors.ErrorNone;
    }

    internal static bool StopInternal(
        out ErrorData err,
        OperationToken token,
        bool forceStop,
        bool clearAllClients = false,
        bool sendEvent = true
    )
    {
        err = Errors.ErrorOperationInProgress;
        if (!@lock.TryAcquire(out var holder))
            return false;

        using var _ = holder;

        err = Errors.ErrorUnexpected.AddMetadata("error", "no client ID found");
        if (!clients.TryRemove(token.ClientId, out var client) && !forceStop)
            return false;

        var adapterEvent = new AdapterEvent();
        var error = adapterEvent.MergeRadioEventData();

        foreach (var c in clearAllClients ? clients.Values : [client])
        {
            if (sendEvent)
            {
                foreach (var device in monitor.CurrentDevices)
                {
                    if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                        Output.ClientEvent(device with { Action = EventAction.Removed }, c.Token);
                }

                if (error == Errors.ErrorNone)
                {
                    Output.ClientEvent(
                        adapterEvent with
                        {
                            OptionDiscovering = false,
                            Action = EventAction.Updated,
                        },
                        c.Token
                    );
                }
            }

            c.Dispose();
        }

        if (clearAllClients)
            clients.Clear();

        if (forceStop)
        {
            monitor.Dispose();
            monitor = new(BluetoothDevice.GetDeviceSelectorFromPairingState(false));
        }

        return true;
    }

    private static DevicesWatcher.Subscriber Subscriber(
        DevicesWatcher monitor,
        OperationToken token
    )
    {
        return new DevicesWatcher.Subscriber(monitor, token)
        {
            OnAdded = static (info, tok) =>
            {
                if (info.OptionPaired.HasValue && !info.OptionPaired.Value)
                    Output.ClientEvent(info, tok);
            },

            OnUpdated = static (oldInfo, newInfo, tok) =>
            {
                if (newInfo.OptionPaired.HasValue && !newInfo.OptionPaired.Value)
                    Output.ClientEvent(newInfo, tok);
            },

            OnRemoved = static (info, tok) =>
            {
                if (info.OptionPaired.HasValue && !info.OptionPaired.Value)
                    Output.ClientEvent(info, tok);
            },
        };
    }
}
