using DotNext.Threading;
using DotNext.Threading.Tasks;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Stack.Base.Monitors;
using Haraltd.Stack.Base.Profiles.Opp;
using InTheHand.Net;
using IOBluetooth;

namespace Haraltd.Stack.Platform.MacOS.Monitors;

public class OppServerMonitor : MonitorObserver
{
    private AdapterOppManager _instance;
    private OperationToken _token;

    private bool _firstStart = true;
    private bool _started;

    private readonly AsyncLock _oppServerLock = AsyncLock.Semaphore(new SemaphoreSlim(1, 1));

    public override void Initialize(OperationToken token)
    {
        _token = OperationManager.GenerateToken(0, token.ClientId);
        StartObjectPushServer().Wait();
    }

    public override void StopAndRelease()
    {
        StopObjectPushServer().Wait();
    }

    private async ValueTask StartObjectPushServer()
    {
        var holder = await _oppServerLock.AcquireAsync(CancellationToken.None);
        using var _ = holder;

        try
        {
            var hostController = HostController.DefaultController;
            if (hostController == null)
                return;

            if (hostController.PowerState != HciPowerState.On)
                return;

            _instance = new AdapterOppManager(
                MacOsStack.OppSessionManager,
                _token,
                BluetoothAddress.Parse(
                    AdapterNativeMethods.GetActualHostControllerAddress(hostController)
                )
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

    private async ValueTask StopObjectPushServer()
    {
        try
        {
            var holder = await _oppServerLock.AcquireAsync(CancellationToken.None);
            using var _ = holder;

            if (!_started)
                return;

            var error = await _instance.StopFileTransferServerAsync();
            if (error == Errors.ErrorNone)
                _started = false;
        }
        catch
        {
            // ignored
        }
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
