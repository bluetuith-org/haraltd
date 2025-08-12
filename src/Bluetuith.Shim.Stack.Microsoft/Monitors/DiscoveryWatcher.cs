using System.Collections.Concurrent;
using Bluetuith.Shim.DataTypes.Events;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Operations.Managers;
using Bluetuith.Shim.Operations.OutputStream;
using Bluetuith.Shim.Stack.Microsoft.Adapters;
using Windows.Devices.Bluetooth;
using static Bluetuith.Shim.DataTypes.Generic.IEvent;
using Lock = DotNext.Threading.Lock;

namespace Bluetuith.Shim.Stack.Microsoft.Monitors;

internal static partial class DiscoveryWatcher
{
    private static readonly ConcurrentDictionary<Guid, DiscoveryClient> Clients = [];

    private static DevicesWatcher _monitor = new(
        BluetoothDevice.GetDeviceSelectorFromPairingState(false)
    );

    private static readonly Lock Lock = Lock.Semaphore(new SemaphoreSlim(1, 1));

    public static bool IsStarted(OperationToken token)
    {
        return Clients.ContainsKey(token.ClientId);
    }

    internal static ErrorData Start(OperationToken token)
    {
        if (!Lock.TryAcquire(out var holder))
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

        if (Clients.ContainsKey(token.ClientId))
            return Errors.ErrorDeviceDiscovery.AddMetadata(
                "exception",
                "Device discovery is already running"
            );

        Clients.TryAdd(token.ClientId, new DiscoveryClient(token, Subscriber(_monitor, token)));

        try
        {
            if (!_monitor.Start())
                throw new Exception("The device discovery service is being reset");

            Output.ClientEvent(
                adapterEvent with
                {
                    OptionDiscovering = true,
                    Action = EventAction.Updated
                },
                token
            );
        }
        catch (Exception e)
        {
            StopInternal(out _, token, true, true, false);
            return Errors.ErrorDeviceDiscovery.AddMetadata("exception", e.Message);
        }

        foreach (var device in _monitor.CurrentDevices)
            if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                Output.ClientEvent(device with { Action = EventAction.Added }, token);

        OperationManager.SetOperationProperties(token, true, true);

        return Errors.ErrorNone;
    }

    internal static ErrorData Stop(OperationToken token)
    {
        if (!StopInternal(out var error, token, false))
            return error;

        if (Clients.IsEmpty)
            _monitor.Stop();

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
        if (!Lock.TryAcquire(out var holder))
            return false;

        using var _ = holder;

        err = Errors.ErrorUnexpected.AddMetadata("error", "no client ID found");
        if (!Clients.TryRemove(token.ClientId, out var client) && !forceStop)
            return false;

        var adapterEvent = new AdapterEvent();
        var error = adapterEvent.MergeRadioEventData();

        foreach (var c in clearAllClients ? Clients.Values : [client])
        {
            if (sendEvent)
            {
                foreach (var device in _monitor.CurrentDevices)
                    if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                        Output.ClientEvent(device with { Action = EventAction.Removed }, c.Token);

                if (error == Errors.ErrorNone)
                    Output.ClientEvent(
                        adapterEvent with
                        {
                            OptionDiscovering = false,
                            Action = EventAction.Updated
                        },
                        c.Token
                    );
            }

            c.Dispose();
        }

        if (clearAllClients)
            Clients.Clear();

        if (forceStop)
        {
            _monitor.Dispose();
            _monitor = new DevicesWatcher(BluetoothDevice.GetDeviceSelectorFromPairingState(false));
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

            OnUpdated = static (_, newInfo, tok) =>
            {
                if (newInfo.OptionPaired.HasValue && !newInfo.OptionPaired.Value)
                    Output.ClientEvent(newInfo, tok);
            },

            OnRemoved = static (info, tok) =>
            {
                if (info.OptionPaired.HasValue && !info.OptionPaired.Value)
                    Output.ClientEvent(info, tok);
            }
        };
    }

    private partial record DiscoveryClient(
        OperationToken Token,
        DevicesWatcher.Subscriber NewSubscriber
    ) : IDisposable
    {
        public void Dispose()
        {
            NewSubscriber?.Dispose();
            Token.Release();
        }
    }
}