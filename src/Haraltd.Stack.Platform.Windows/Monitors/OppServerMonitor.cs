using DotNext.Threading;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Stack.Base.Monitors;
using Haraltd.Stack.Base.Profiles.Opp;
using Windows.Devices.Bluetooth;
using Haraltd.Operations.Managers;

namespace Haraltd.Stack.Platform.Windows.Monitors;

internal partial class OppServerMonitor : MonitorObserver
{
    private AdapterOppManager _instance = new();
    private OperationToken _token;

    private bool _firstStart = true;
    private bool _started;

    private readonly AsyncLock _oppServerLock = AsyncLock.Semaphore(new SemaphoreSlim(1, 1));

    public override void Initialize(OperationToken token)
    {
        _token = OperationManager.GenerateToken(0, token.ClientId);
        StartObjectPushServer().Wait();
    }

    private async Task StartObjectPushServer()
    {
        var holder = await _oppServerLock.AcquireAsync(CancellationToken.None);
        using var _ = holder;

        if (_started)
            return;

        try
        {
            var adapter = BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult();
            if (adapter == null)
                return;

            _instance = new AdapterOppManager(
                WindowsStack.OppSessionManager,
                _token,
                adapter.BluetoothAddress
            );

            if (_firstStart)
                Console.WriteLine("[+] Registering OBEX service, please wait...");

            var error = await _instance.StartFileTransferServerAsync();

            if (_firstStart)
            {
                Console.WriteLine(
                    error == Errors.ErrorNone
                        ? "[+] OBEX service started."
                        : "[+] Could not start OBEX service, will retry later."
                );
            }

            _started = error == Errors.ErrorNone;
        }
        catch
        {
            // ignored
        }
        finally
        {
            _firstStart = false;
        }
    }

    private async Task StopObjectPushServer()
    {
        var holder = await _oppServerLock.AcquireAsync(CancellationToken.None);
        using var _ = holder;

        if (!_started)
            return;

        await _instance.StopFileTransferServerAsync();
        _started = false;
    }

    public override void AdapterEventHandler(AdapterEvent adapterEvent)
    {
        if (
            adapterEvent.Action == IEvent.EventAction.Removed
            || adapterEvent.OptionPowered == false
        )
            Task.Run(StopObjectPushServer);

        if (adapterEvent.Action == IEvent.EventAction.Added || adapterEvent.OptionPowered == true)
            Task.Run(StartObjectPushServer);
    }
}
