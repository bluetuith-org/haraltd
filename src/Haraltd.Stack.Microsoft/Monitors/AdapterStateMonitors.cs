using System.Timers;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using InTheHand.Net;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Win32;
using Timer = System.Timers.Timer;

namespace Haraltd.Stack.Microsoft.Monitors;

internal partial class AdapterStateMonitor : IMonitor
{
    protected readonly AdapterEvent AdapterEvent = new();
    protected OperationToken Token;
    private BluetoothAdapter _bluetoothAdapter;

    protected Radio Radio;

    public BluetoothAddress AdapterAddress =>
        _bluetoothAdapter != null
            ? new BluetoothAddress(_bluetoothAdapter.BluetoothAddress)
            : throw new InvalidDataException("Uninitialized adapter");

    public virtual MonitorName Name => MonitorName.None;
    public virtual bool IsRunning => false;
    public virtual bool IsCreated => false;
    public virtual bool RestartOnPowerStateChanged => false;

    public virtual void Create(OperationToken token)
    {
        Token = token;

        _bluetoothAdapter =
            BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult()
            ?? throw new NullReferenceException("No adapter was found");
        Radio =
            _bluetoothAdapter.GetRadioAsync().GetAwaiter().GetResult()
            ?? throw new NullReferenceException("No radio was found or is disabled");

        AdapterEvent.Address = AdapterAddress.ToString("C");
        AdapterEvent.Action = IEvent.EventAction.Updated;
    }

    public virtual bool Start()
    {
        return true;
    }

    public virtual void Stop() { }

    public virtual void Dispose() { }
}

internal partial class AdapterPowerStateMonitor : AdapterStateMonitor
{
    private bool _started;

    public override MonitorName Name => MonitorName.AdapterPowerStateMonitor;
    public override bool IsRunning => Radio != null && _started;
    public override bool IsCreated => Radio != null;
    public override bool RestartOnPowerStateChanged => false;

    public override bool Start()
    {
        if (base.Start() && Radio != null)
        {
            Radio.StateChanged += Push_Powered_Event;
            _started = true;
        }

        return _started;
    }

    public override void Stop()
    {
        if (Radio != null)
            Radio.StateChanged -= Push_Powered_Event;

        _started = false;

        base.Stop();
    }

    public override void Dispose()
    {
        Stop();

        base.Dispose();
    }

    internal virtual void Push_Powered_Event(Radio radio, object args)
    {
        if (AdapterEvent.OptionPowered == false && radio.State == RadioState.On)
            AdapterEvent.OptionDiscoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
        else
            AdapterEvent.OptionDiscoverable = null;

        AdapterEvent.OptionPowered = AdapterEvent.OptionPairable = radio.State == RadioState.On;

        Output.Event(AdapterEvent, Token);
    }
}

internal partial class AdapterDiscoverableStateMonitor : AdapterStateMonitor
{
    private Timer _timer;

    public override MonitorName Name => MonitorName.AdapterDiscoverableStateMonitor;
    public override bool IsRunning => Radio != null && _timer is { Enabled: true };
    public override bool IsCreated => Radio != null && _timer != null;
    public override bool RestartOnPowerStateChanged => false;

    public override void Create(OperationToken token)
    {
        base.Create(token);

        _timer = new Timer(TimeSpan.FromSeconds(1)) { AutoReset = true };

        AdapterEvent.OptionDiscoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
    }

    public override bool Start()
    {
        if (base.Start() && _timer != null)
        {
            _timer.Elapsed += Push_Discovery_Event;
            _timer.Start();
        }

        return IsRunning;
    }

    public override void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= Push_Discovery_Event;
        }

        base.Stop();
    }

    internal void PushEvent()
    {
        if (_timer != null)
        {
            _timer.Stop();
            Push_Discovery_Event(null, null);
            _timer.Start();
        }
    }

    private void Push_Discovery_Event(object sender, ElapsedEventArgs e)
    {
        if (Radio == null)
            return;

        var discoverable =
            Radio.State == RadioState.On && PInvoke.BluetoothIsDiscoverable(null) > 0;
        if (
            AdapterEvent.OptionDiscoverable.HasValue
            && AdapterEvent.OptionDiscoverable.Value == discoverable
        )
            return;

        AdapterEvent.OptionDiscoverable = discoverable;
        Output.Event(AdapterEvent, Token);
    }

    public override void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();

        base.Dispose();
    }
}
