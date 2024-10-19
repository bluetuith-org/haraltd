using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Executor.OutputHandler;
using Bluetuith.Shim.Stack.Events;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Stack.Providers.MSFT.Devices;
using Bluetuith.Shim.Types;
using InTheHand.Net.Sockets;
using Nefarius.Utilities.Bluetooth;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Adapters;

internal sealed class AdapterMethods
{

    public static async Task<ErrorData> DisconnectDeviceAsync(string address)
    {
        try
        {
            ThrowIfRadioNotOperable();

            using HostRadio radio = new();
            radio.DisconnectRemoteDevice(address);
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceDisconnect.WrapError(new() {
                {"exception",  e.Message}
            });
        }

        return await Task.FromResult(Errors.ErrorNone);
    }

    public static async Task<(GenericResult<List<DeviceModel>>, ErrorData)> GetPairedDevices()
    {
        List<DeviceModel> pairedDevices = [];

        try
        {
            ThrowIfRadioNotOperable();

            foreach (BluetoothDeviceInfo? device in Client.Handle.PairedDevices)
            {
                if (device == null)
                    continue;

                pairedDevices.Add(new DeviceModelExt(device));
            }
        }
        catch (Exception e)
        {
            return (GenericResult<List<DeviceModel>>.Empty(), StackErrors.ErrorDeviceNotFound.WrapError(new()
            {
                {"exception", e.Message }
            }));
        }

        return await Task.FromResult((pairedDevices.ToResult("Paired Devices:", "pairedDevices"), Errors.ErrorNone));
    }

    public static async Task<ErrorData> RemoveDeviceAsync(string address)
    {
        try
        {
            using BluetoothDevice device = await DeviceUtils.GetBluetoothDevice(address);
            if (!device.DeviceInformation.Pairing.IsPaired)
            {
                return Errors.ErrorNone;
            }

            DeviceUnpairingResult unpairResult = await device.DeviceInformation.Pairing.UnpairAsync();
            if (unpairResult.Status != DeviceUnpairingResultStatus.Unpaired)
            {
                throw new Exception($"Could not unpair device {address}: {unpairResult.Status}");
            }
        }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceUnpairing.WrapError(new()
            {
                {"device-address", address },
                {"exception", e.Message}
            });
        }

        return Errors.ErrorNone;
    }
    public static async Task<ErrorData> SetAdapterStateAsync(bool enable)
    {
        try
        {
            var isOperable = HostRadio.IsOperable;

            if (enable && isOperable || !enable && !isOperable)
            {
                return Errors.ErrorNone;
            }

            using HostRadio hostRadio = new();
            if (!enable)
            {
                hostRadio.DisableRadio();
            }
        }
        catch (Exception e)
        {
            return StackErrors.ErrorAdapterPowerModeAccess.WrapError(new()
            {
                {"exception", e.Message}
            });
        }

        return await Task.FromResult(Errors.ErrorNone);
    }

    public static async Task<ErrorData> StartDeviceDiscoveryAsync(OperationToken token, int timeout = 0)
    {
        DiscoveryEvent discoveryEvent = new(DiscoveryEvent.DiscoveryStatus.Started);

        try
        {
            if (timeout > 0)
            {
                token.CancelTokenSource.CancelAfter(timeout);
            }

            Output.Event(discoveryEvent, token);
            discoveryEvent.Status = DiscoveryEvent.DiscoveryStatus.InProgress;

            TypedEventHandler<DeviceWatcher, DeviceInformation> addedEvent = new(async (s, e) =>
            {
                DeviceModel device = DeviceModelExt.ConvertToDeviceModel(await BluetoothDevice.FromIdAsync(e.Id));
                Output.Event(discoveryEvent with { Device = device, DeviceStatus = DiscoveryEvent.DeviceInfoStatus.Added }, token);
            });
            TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> removedEvent = new(async (s, e) =>
            {
                DeviceModel device = DeviceModelExt.ConvertToDeviceModel(await BluetoothDevice.FromIdAsync(e.Id));
                Output.Event(discoveryEvent with { Device = device, DeviceStatus = DiscoveryEvent.DeviceInfoStatus.Removed }, token);
            });
            TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> updatedEvent = new(async (s, e) =>
            {
                DeviceModel device = DeviceModelExt.ConvertToDeviceModel(await BluetoothDevice.FromIdAsync(e.Id));
                Output.Event(discoveryEvent with { Device = device, DeviceStatus = DiscoveryEvent.DeviceInfoStatus.Updated }, token);
            });

            DeviceWatcher watcher = DeviceInformation.CreateWatcher(BluetoothDevice.GetDeviceSelectorFromPairingState(false));
            watcher.Added += addedEvent;
            watcher.Removed += removedEvent;
            watcher.Updated += updatedEvent;
            watcher.Start();

            token.CancelToken.WaitHandle.WaitOne();

            watcher.Stop();
            watcher.Added -= addedEvent;
            watcher.Removed -= removedEvent;
            watcher.Updated -= updatedEvent;
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            return StackErrors.ErrorDeviceDiscovery.WrapError(new()
            {
                {"exception", e.Message},
            });
        }
        finally
        {
            Output.Event(discoveryEvent with { Status = DiscoveryEvent.DiscoveryStatus.Stopped }, token);
        }

        return await Task.FromResult(Errors.ErrorNone);
    }


    public static void ThrowIfRadioNotOperable()
    {
        if (!HostRadio.IsOperable)
            throw new Exception("The host radio is not powered on");
    }
}