using System.Collections.Concurrent;
using System.Timers;
using DotNext.Threading;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Microsoft.Devices;
using Windows.Devices.Enumeration;
using static Haraltd.DataTypes.Generic.IEvent;
using Timer = System.Timers.Timer;

namespace Haraltd.Stack.Microsoft.Monitors;

internal partial class DevicesWatcher : IWatcher
{
    private readonly Timer _stopTimer;

    private readonly DeviceWatcher _watcher;
    private readonly Dictionary<string, DeviceChanges> _devices = [];
    private readonly ManualResetEvent _mre = new(true);
    private readonly ConcurrentDictionary<long, Subscriber> _subscribers = [];

    internal DevicesWatcher(string aqs)
    {
        _stopTimer = new Timer(TimeSpan.FromSeconds(10));
        _stopTimer.Elapsed += StopTimer_Elapsed;
        _stopTimer.AutoReset = false;

        _watcher = DeviceInformation.CreateWatcher(
            aqs,
            new List<string>
            {
                "System.ItemNameDisplay",
                "System.Devices.Aep.AepId",
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.IsPaired",
                "System.Devices.Aep.Category",
                "System.Devices.Aep.Manufacturer",
                "System.Devices.Aep.SignalStrength",
                "System.Devices.Aep.Bluetooth.Cod.Major",
                "System.Devices.Aep.Bluetooth.Cod.Minor",
                "System.Devices.Aep.Bluetooth.Cod.Services.Audio",
                "System.Devices.Aep.Bluetooth.Cod.Services.Capturing",
                "System.Devices.Aep.Bluetooth.Cod.Services.Information",
                "System.Devices.Aep.Bluetooth.Cod.Services.LimitedDiscovery",
                "System.Devices.Aep.Bluetooth.Cod.Services.Networking",
                "System.Devices.Aep.Bluetooth.Cod.Services.ObjectXfer",
                "System.Devices.Aep.Bluetooth.Cod.Services.Positioning",
                "System.Devices.Aep.Bluetooth.Cod.Services.Rendering",
                "System.Devices.Aep.Bluetooth.Cod.Services.Telephony",
            },
            DeviceInformationKind.AssociationEndpointContainer
        );
    }

    internal IEnumerable<DeviceChanges> CurrentDevices
    {
        get
        {
            List<DeviceChanges> changes = [];

            using (LockAcquisition.AcquireReadLock(_devices))
            {
                foreach (var device in _devices.Values)
                    changes.Add(device with { });
            }

            return changes;
        }
    }

    public bool IsCreated => _watcher != null;

    public bool IsRunning =>
        _watcher != null
        && (
            _watcher.Status == DeviceWatcherStatus.Started
            || _watcher.Status == DeviceWatcherStatus.EnumerationCompleted
            || _watcher.Status == DeviceWatcherStatus.Created
        );

    public bool Start()
    {
        if (!_mre.WaitOne(TimeSpan.FromSeconds(1)))
            return false;

        _stopTimer.Stop();

        if (_watcher.Status == DeviceWatcherStatus.Created)
        {
            _watcher.Added += Watcher_Added;
            _watcher.Updated += Watcher_Updated;
            _watcher.Removed += Watcher_Removed;
            _watcher.Stopped += Watcher_Stopped;

            goto StartWatcher;
        }

        if (
            _watcher.Status != DeviceWatcherStatus.Stopped
            && _watcher.Status != DeviceWatcherStatus.Aborted
        )
            return true;

        StartWatcher:
        _watcher.Start();

        return true;
    }

    public void Stop()
    {
        _stopTimer.Stop();
        _stopTimer.Start();
    }

    public void Dispose()
    {
        try
        {
            _watcher.Stop();
            _stopTimer.Stop();
        }
        catch { }
        finally
        {
            _stopTimer.Elapsed -= StopTimer_Elapsed;
            _watcher.Added -= Watcher_Added;
            _watcher.Updated -= Watcher_Updated;
            _watcher.Removed -= Watcher_Removed;

            _stopTimer.Dispose();
        }
    }

    private void StopTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (
            _watcher.Status != DeviceWatcherStatus.Started
            && _watcher.Status != DeviceWatcherStatus.EnumerationCompleted
        )
            return;

        _mre.Reset();

        _watcher.Stop();
    }

    private void Watcher_Stopped(DeviceWatcher sender, object args)
    {
        ClearDevices();
        _mre.Set();
    }

    private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        RemoveDeviceInformation(args);
    }

    private void Watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
    {
        UpdateDeviceInformation(args);
    }

    private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
    {
        AddDeviceInformation(args);
    }

    internal void ClearDevices()
    {
        using (LockAcquisition.AcquireWriteLock(_devices))
        {
            _devices.Clear();
        }
    }

    internal void AddDeviceInformation(DeviceInformation deviceInformation)
    {
        var changes = new DeviceChanges(deviceInformation);

        using (LockAcquisition.AcquireWriteLock(_devices))
        {
            _devices[deviceInformation.Id] = changes with { };
        }

        PushEvents(EventAction.Added, changes, null);
    }

    internal void UpdateDeviceInformation(DeviceInformationUpdate deviceInformationUpdate)
    {
        DeviceChanges oldChanges = null,
            newChanges = null;

        using (LockAcquisition.AcquireWriteLock(_devices))
        {
            if (_devices.TryGetValue(deviceInformationUpdate.Id, out var device))
            {
                oldChanges = device with { Action = EventAction.Added };

                device.UpdateInformation(deviceInformationUpdate);

                newChanges = device with { Action = EventAction.Updated };
            }
        }

        PushEvents(EventAction.Updated, oldChanges, newChanges);
    }

    internal void RemoveDeviceInformation(DeviceInformationUpdate deviceInformationUpdate)
    {
        DeviceChanges currentInfo;

        using (LockAcquisition.AcquireWriteLock(_devices))
        {
            _devices.Remove(deviceInformationUpdate.Id, out currentInfo);
        }

        if (currentInfo == null)
            return;

        currentInfo.Action = EventAction.Removed;

        PushEvents(EventAction.Removed, currentInfo, null);
    }

    private void PushEvents(
        EventAction eventAction,
        DeviceChanges oldChanges,
        DeviceChanges newChanges
    )
    {
        if (oldChanges == null || (eventAction == EventAction.Updated && newChanges == null))
            return;

        foreach (var subscriber in _subscribers.Values)
            switch (eventAction)
            {
                case EventAction.Added:
                    subscriber.OnAdded(oldChanges, subscriber.Token);
                    break;
                case EventAction.Updated:
                    subscriber.OnUpdated(oldChanges, newChanges, subscriber.Token);
                    break;
                case EventAction.Removed:
                    subscriber.OnRemoved(oldChanges, subscriber.Token);
                    break;
            }
    }

    internal void RegisterSubscriber(Subscriber subscriber)
    {
        _subscribers.TryAdd(subscriber.Id, subscriber);
    }

    internal void UnregisterSubscriber(Subscriber subscriber)
    {
        _subscribers.TryRemove(subscriber.Id, out _);
    }

    internal partial class Subscriber : IDisposable
    {
        private readonly DevicesWatcher _monitor;
        internal Action<DeviceChanges, OperationToken> OnAdded = static delegate { };
        internal Action<DeviceChanges, OperationToken> OnRemoved = static delegate { };

        internal Action<DeviceChanges, DeviceChanges, OperationToken> OnUpdated = static delegate
        { };

        internal Subscriber(DevicesWatcher monitor, OperationToken token)
        {
            _monitor = monitor;
            Token = token;

            _monitor?.RegisterSubscriber(this);
        }

        internal long Id => Token.OperationId;
        internal OperationToken Token { get; }

        public void Dispose()
        {
            _monitor?.UnregisterSubscriber(this);
        }
    }

    internal record DeviceChanges : DeviceEvent
    {
        private readonly DeviceInformation _deviceInformation;

        internal DeviceChanges(DeviceInformation deviceInformation)
        {
            _deviceInformation = deviceInformation;
            this.MergeDeviceInformation(_deviceInformation);
        }

        internal bool UpdateInformation(
            DeviceInformationUpdate deviceInformationUpdate,
            bool appendServices = false
        )
        {
            if (_deviceInformation == null)
                return false;

            _deviceInformation.Update(deviceInformationUpdate);
            return this.MergeDeviceInformation(_deviceInformation, appendServices);
        }

        internal bool RefreshInformation()
        {
            if (_deviceInformation == null)
                return false;

            return this.MergeDeviceInformation(
                _deviceInformation,
                _deviceInformation.Pairing.IsPaired
            );
        }
    }
}
