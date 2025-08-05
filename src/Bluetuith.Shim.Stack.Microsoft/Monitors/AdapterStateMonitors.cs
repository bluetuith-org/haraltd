using Bluetuith.Shim.DataTypes;
using Bluetuith.Shim.Operations;
using Bluetuith.Shim.Stack.Microsoft;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;
using Windows.Win32;
using Timer = System.Timers.Timer;

namespace Bluetuith.Shim.Monitors;

internal class AdapterStateMonitor
{
    protected readonly AdapterEvent adapterEvent = new();
    protected readonly OperationToken _token;
    protected Radio radio;

    internal AdapterStateMonitor(OperationToken token)
    {
        _token = token;

        var adapterData = new AdapterModel();
        var error = adapterData.MergeRadioEventData();
        if (error != Errors.ErrorNone)
            throw new Exception(error.Description);

        adapterEvent.Address = adapterData.Address;
        adapterEvent.Action = IEvent.EventAction.Updated;
    }

    internal virtual void Start() { }

    internal virtual void Stop() { }
}

internal class AdapterPowerStateMonitor : AdapterStateMonitor
{
    internal AdapterPowerStateMonitor(OperationToken token)
        : base(token) { }

    internal override void Start()
    {
        var adapter =
            BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult()
            ?? throw new NullReferenceException("No adapter was found");
        radio =
            adapter.GetRadioAsync().GetAwaiter().GetResult()
            ?? throw new NullReferenceException("No radio was found or is disabled");

        radio.StateChanged += Radio_StateChanged;
    }

    internal override void Stop()
    {
        if (radio != null)
            radio.StateChanged -= Radio_StateChanged;
    }

    private void Radio_StateChanged(Windows.Devices.Radios.Radio radio, object args)
    {
        if (adapterEvent.OptionPowered == false && radio.State == RadioState.On)
            adapterEvent.OptionDiscoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
        else
            adapterEvent.OptionDiscoverable = null;

        adapterEvent.OptionPowered = adapterEvent.OptionPairable = radio.State == RadioState.On;

        Output.Event(adapterEvent, _token);
    }
}

internal class AdapterDiscoverableStateMonitor : AdapterStateMonitor
{
    private readonly Timer timer = new(TimeSpan.FromSeconds(1));

    internal AdapterDiscoverableStateMonitor(OperationToken token)
        : base(token)
    {
        timer.AutoReset = true;
        adapterEvent.OptionDiscoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
    }

    private void Push_Discovery_Event(object sender, System.Timers.ElapsedEventArgs e)
    {
        var discoverable = PInvoke.BluetoothIsDiscoverable(null) > 0;
        if (
            adapterEvent.OptionDiscoverable.HasValue
            && adapterEvent.OptionDiscoverable.Value == discoverable
        )
            return;

        adapterEvent.OptionDiscoverable = discoverable;
        Output.Event(adapterEvent, _token);
    }

    internal override void Start()
    {
        timer.Elapsed += Push_Discovery_Event;
        timer.Start();
    }

    internal override void Stop()
    {
        timer.Stop();
        timer.Elapsed -= Push_Discovery_Event;
    }

    internal void PushEvent()
    {
        timer.Stop();
        Push_Discovery_Event(null, null);
        timer.Start();
    }
}

internal static class AdapterDiscoverableEventMonitor
{
    private static AdapterDiscoverableStateMonitor discoverableMonitor;

    internal static void Start(OperationToken token)
    {
        discoverableMonitor = new(token);
        discoverableMonitor.Start();
    }

    internal static void Stop() => discoverableMonitor?.Stop();

    internal static void PushDiscoverableEvent() => discoverableMonitor?.PushEvent();
}
