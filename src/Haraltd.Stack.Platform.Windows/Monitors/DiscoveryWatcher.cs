using System.Collections.Concurrent;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack.Base.Monitors;
using Haraltd.Stack.Platform.Windows.Adapters;
using Windows.Devices.Bluetooth;
using static Haraltd.DataTypes.Generic.IEvent;
using Lock = DotNext.Threading.Lock;

namespace Haraltd.Stack.Platform.Windows.Monitors;

internal static partial class DiscoveryWatcher
{
    internal static readonly DiscoveryStateMonitor Instance = new();

    private static readonly ConcurrentDictionary<Guid, DiscoveryClient> Clients = [];

    private static DevicesWatcher _monitor = new(
        BluetoothDevice.GetDeviceSelectorFromPairingState(false)
    );

    private static readonly Lock Lock = Lock.Semaphore(new SemaphoreSlim(1, 1));
    private static AdapterEvent _adapterEvent = new();

    public static bool IsStarted(OperationToken token)
    {
        return Clients.ContainsKey(token.ClientId);
    }

    internal static ErrorData Start(OperationToken token)
    {
        if (!Lock.TryAcquire(out var holder))
            return Errors.ErrorOperationInProgress;

        using var sem = holder;

        if (string.IsNullOrEmpty(_adapterEvent.Address))
        {
            _adapterEvent = WindowsAdapterExtensions.GetEmptyAdapterEventData(out var err);
            if (err != Errors.ErrorNone)
                return err;
        }

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

        try
        {
            _monitor ??= new DevicesWatcher(
                BluetoothDevice.GetDeviceSelectorFromPairingState(false)
            );

            Clients.TryAdd(token.ClientId, new DiscoveryClient(token, Subscriber(_monitor, token)));

            if (!_monitor.Start())
                throw new Exception("The device discovery service is being reset");

            Output.ClientEvent(
                _adapterEvent with
                {
                    OptionDiscovering = true,
                    Action = EventAction.Updated,
                },
                token
            );
        }
        catch (Exception e)
        {
            Stop(token, false);
            return Errors.ErrorDeviceDiscovery.AddMetadata("exception", e.Message);
        }

        foreach (var device in _monitor.CurrentDevices)
            if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                Output.ClientEvent(device with { Action = EventAction.Added }, token);

        OperationManager.SetOperationProperties(token, true, true);

        return Errors.ErrorNone;
    }

    internal static ErrorData Stop(OperationToken token, bool shouldLock = true)
    {
        Lock.Holder holder = default;

        if (shouldLock)
        {
            if (!Lock.TryAcquire(out holder))
                return Errors.ErrorOperationInProgress;
        }

        if (!Clients.TryRemove(token.ClientId, out var client))
            return Errors.ErrorUnexpected.AddMetadata("error", "no client ID found");

        token = client.Token;
        client.Dispose();

        foreach (var device in _monitor.CurrentDevices)
            if (device.OptionPaired.HasValue && !device.OptionPaired.Value)
                Output.ClientEvent(device with { Action = EventAction.Removed }, token);

        Output.ClientEvent(
            _adapterEvent with
            {
                OptionDiscovering = false,
                Action = EventAction.Updated,
            },
            token
        );

        if (Clients.IsEmpty)
            _monitor.Stop();

        if (holder != false)
            holder.Dispose();

        return Errors.ErrorNone;
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
            },
        };
    }

    internal partial class DiscoveryStateMonitor : MonitorObserver
    {
        public override void AdapterEventHandler(AdapterEvent adapterEvent)
        {
            if (adapterEvent.Action == EventAction.Removed || adapterEvent.OptionPowered == false)
            {
                using var _ = Lock.Acquire();

                foreach (var (_, client) in Clients)
                {
                    Stop(client.Token, false);
                }

                _monitor?.Dispose();
                _monitor = null;
            }
        }
    }

    private partial record DiscoveryClient(
        OperationToken Token,
        DevicesWatcher.Subscriber NewSubscriber
    ) : IDisposable
    {
        public void Dispose()
        {
            NewSubscriber?.Dispose();
        }
    }
}
