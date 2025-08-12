using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Timers;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.OperationToken;
using Bluetuith.Shim.Operations.OutputStream;
using Windows.Devices.Radios;
using Timer = System.Timers.Timer;

namespace Bluetuith.Shim.Stack.Microsoft.Monitors;

internal static partial class WindowsMonitors
{
    private static readonly ManualResetEvent MonitorStateMre = new(true);
    private static bool _monitorRunning;
    private static bool _monitorStarted;

    private static readonly ManualResetEvent AdapterStateMre = new(false);

    private static readonly AdapterEventMonitor AdapterEventMonitorSession = new(
        PowerStateChangeAction
    );

    private static readonly ConcurrentDictionary<MonitorName, MonitorTaskRecord> Tasks = new();

    private static readonly FrozenSet<IMonitor> Monitors =
    [
        new DevicesStateMonitor(),
        new DevicesBatteryStateMonitor(),
        new OppServerMonitor()
    ];

    private static Timer _timer;

    public static ErrorData Start(OperationToken token)
    {
        if (_monitorStarted)
            return Errors.ErrorNone;

        try
        {
            AdapterEventMonitorSession.Create(token);
            AdapterEventMonitorSession.Start();

            _timer = new Timer(TimeSpan.FromSeconds(2)) { AutoReset = false };
            _timer.Elapsed += Timer_Elapsed;

            AdapterEventMonitorSession.PushPoweredEvent(false);
        }
        catch (Exception e)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", e.Message);
        }

        _monitorStarted = true;

        return Errors.ErrorNone;
    }

    public static ErrorData Stop()
    {
        if (!_monitorStarted)
            return Errors.ErrorNone;

        try
        {
            MonitorStateMre.WaitOne();
            MonitorStateMre.Reset();

            _monitorRunning = false;
            _monitorStarted = false;

            _timer.Stop();
            _timer.Dispose();

            foreach (var mrecord in Tasks.Values)
                try
                {
                    mrecord.Monitor.Stop();
                    mrecord.Monitor.Dispose();
                }
                catch
                {
                }

            Tasks.Clear();

            AdapterEventMonitorSession.Stop();
            AdapterEventMonitorSession.Dispose();
        }
        catch (Exception e)
        {
            return Errors.ErrorUnexpected.AddMetadata("exception", e.Message);
        }
        finally
        {
            MonitorStateMre.Set();
        }

        return Errors.ErrorNone;
    }

    public static void PushDiscoverableEvent()
    {
        AdapterEventMonitorSession.PushDiscoverableEvent();
    }

    private static async Task LaunchNewMonitor(object state)
    {
        if (state is not MonitorTaskRecord mrecord)
            return;

        Restart:
        try
        {
            if (Tasks.TryGetValue(mrecord.Monitor.Name, out var mrec))
            {
                if (mrec.Monitor.IsRunning)
                    return;

                mrec.Token = mrecord.Token;
                mrecord = mrec;
            }

            AdapterStateMre.WaitOne();
            MonitorStateMre.WaitOne();

            if (!_monitorRunning)
                return;

            if (!mrecord.Monitor.IsCreated)
                mrecord.Monitor.Create(mrecord.Token);

            if (!mrecord.Monitor.Start())
                throw new Exception($"Cannot start monitor {mrecord.Monitor.Name}");

            Tasks.TryAdd(mrecord.Monitor.Name, mrecord);
        }
        catch (Exception e)
        {
            mrecord.ExceptionCount++;

            if (mrecord.ExceptionCount > 3)
            {
                Output.Event(
                    Errors.ErrorUnexpected.AddMetadata(
                        $"An error occurred while starting a monitor: {mrecord.Monitor.Name}",
                        e.Message
                    ),
                    mrecord.Token
                );
                Tasks.TryRemove(mrecord.Monitor.Name, out _);

                return;
            }

            await Task.Delay(1000);
            goto Restart;
        }
    }

    private static void RestartMonitors(OperationToken token, RadioState radioState)
    {
        try
        {
            if (!MonitorStateMre.WaitOne(TimeSpan.FromSeconds(1)))
                return;

            MonitorStateMre.Reset();

            switch (radioState)
            {
                case RadioState.On:
                    StopAllMonitors();
                    StartAllMonitors();
                    break;

                case RadioState.Unknown:
                case RadioState.Off:
                case RadioState.Disabled:
                default:
                    StopAllMonitors();
                    break;
            }
        }
        catch
        {
        }
        finally
        {
            MonitorStateMre.Set();
        }

        return;

        void StartAllMonitors()
        {
            _monitorRunning = true;

            foreach (var monitor in Monitors)
                Task.Factory.StartNew(
                    LaunchNewMonitor,
                    new MonitorTaskRecord { Token = token, Monitor = monitor }
                );
        }

        void StopAllMonitors()
        {
            try
            {
                foreach (var monitor in Monitors)
                    if (
                        Tasks.TryGetValue(monitor.Name, out var mrecord)
                        && mrecord.Monitor.RestartOnPowerStateChanged
                    )
                        try
                        {
                            mrecord.Monitor.Stop();
                        }
                        catch
                        {
                            mrecord.Monitor.Dispose();
                        }
                        finally
                        {
                            Tasks.TryRemove(mrecord.Monitor.Name, out _);
                        }
            }
            catch
            {
            }
            finally
            {
                _monitorRunning = false;
            }
        }
    }

    private static void PowerStateChangeAction(Radio radio, object args)
    {
        switch (radio.State)
        {
            case RadioState.On:
                AdapterStateMre.Set();
                break;

            case RadioState.Off:
            case RadioState.Unknown:
            case RadioState.Disabled:
                AdapterStateMre.Reset();
                DiscoveryWatcher.StopInternal(
                    out _,
                    AdapterEventMonitorSession.CurrentToken,
                    true,
                    true
                );
                break;
        }

        PushDiscoverableEvent();

        _timer.Stop();
        _timer.Start();
    }

    private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        RestartMonitors(
            AdapterEventMonitorSession.CurrentToken,
            AdapterEventMonitorSession.AdapterPowerState
        );
    }

    private record MonitorTaskRecord
    {
        public int ExceptionCount;

        public IMonitor Monitor;
        public OperationToken Token;
    }

    private partial class AdapterEventMonitor(Action<Radio, object> action)
        : AdapterPowerStateMonitor
    {
        private readonly AdapterDiscoverableStateMonitor _discoverableMonitor = new();

        internal OperationToken CurrentToken => Token;

        internal RadioState AdapterPowerState => Radio != null ? Radio.State : RadioState.Unknown;

        public override void Create(OperationToken token)
        {
            base.Create(token);
            _discoverableMonitor.Create(token);
        }

        public override bool Start()
        {
            return base.Start() && _discoverableMonitor.Start();
        }

        public override void Stop()
        {
            _discoverableMonitor.Stop();
            base.Stop();
        }

        public override void Dispose()
        {
            _discoverableMonitor.Dispose();
            base.Dispose();
        }

        internal void PushPoweredEvent(bool publishEvent = true)
        {
            Push_Powered_Event(Radio, publishEvent);
        }

        internal void PushDiscoverableEvent()
        {
            _discoverableMonitor.PushEvent();
        }

        internal override void Push_Powered_Event(Radio radio, object args)
        {
            action(radio, args);

            if (args is false)
                return;

            base.Push_Powered_Event(radio, args);
        }
    }
}