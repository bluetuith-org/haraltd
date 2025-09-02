using System.Runtime.InteropServices;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations;
using Haraltd.Stack.Base;
using Haraltd.Stack.Microsoft.Adapters;
using Haraltd.Stack.Microsoft.Devices;
using Haraltd.Stack.Microsoft.Devices.Profiles;
using static Haraltd.DataTypes.Models.Features;

namespace Haraltd.Stack.Microsoft;

public class MsftStack : IBluetoothStack
{
    public ErrorData StartMonitors(OperationToken token)
    {
        return Monitors.WindowsMonitors.Start(token);
    }

    public ErrorData StopMonitors()
    {
        return Monitors.WindowsMonitors.Stop();
    }

    public PlatformInfo GetPlatformInfo()
    {
        return new PlatformInfo
        {
            Stack = "Microsoft MSFT",
            OsInfo = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        };
    }

    public Features GetFeatureFlags()
    {
        return new Features(
            FeatureFlags.FeatureConnection
                | FeatureFlags.FeaturePairing
                | FeatureFlags.FeatureSendFile
                | FeatureFlags.FeatureReceiveFile
        );
    }

    #region Adapter Methods

    public (AdapterModel, ErrorData) GetAdapter(OperationToken token)
    {
        return AdapterModelExt.ConvertToAdapterModel(token);
    }

    public ErrorData SetPoweredState(bool enable)
    {
        return AdapterMethods.SetPoweredState(enable);
    }

    public ErrorData SetPairableState(bool enable)
    {
        return AdapterMethods.SetPairableState(enable);
    }

    public ErrorData SetDiscoverableState(bool enable)
    {
        return AdapterMethods.SetDiscoverableState(enable);
    }

    public (GenericResult<List<DeviceModel>>, ErrorData) GetPairedDevices()
    {
        return AdapterMethods.GetPairedDevices();
    }

    public ErrorData StartDeviceDiscovery(OperationToken token, int timeout = 0)
    {
        return AdapterMethods.StartDeviceDiscovery(token, timeout);
    }

    public ErrorData StopDeviceDiscovery(OperationToken token)
    {
        return AdapterMethods.StopDeviceDiscovery(token);
    }

    public ErrorData DisconnectDevice(string address)
    {
        return AdapterMethods.DisconnectDevice(address);
    }

    public Task<ErrorData> RemoveDeviceAsync(string address)
    {
        return AdapterMethods.RemoveDeviceAsync(address);
    }

    #endregion

    #region Device Methods

    public (DeviceModel, ErrorData) GetDevice(OperationToken token, string address)
    {
        return DeviceModelExt.ConvertToDeviceModel(address);
    }

    public ErrorData Connect(OperationToken token, string address)
    {
        return Connection.Connect(token, address);
    }

    public ErrorData ConnectProfile(OperationToken token, string address, Guid profileGuid)
    {
        return Connection.ConnectProfile(token, address, profileGuid);
    }

    public ErrorData Disconnect(OperationToken token, string address)
    {
        return Connection.Disconnect(token, address);
    }

    public ErrorData DisconnectProfile(OperationToken token, string address, Guid profileGuid)
    {
        return Connection.DisconnectProfile(token, address, profileGuid);
    }

    public Task<ErrorData> PairAsync(OperationToken token, string address, int timeout = 10)
    {
        return new Pairing().PairAsync(token, address, timeout);
    }

    public ErrorData CancelPairing(string address)
    {
        return Pairing.CancelPairing(address);
    }

    // Advanced Audio Distribution (A2DP) based operations
    public Task<ErrorData> StartAudioSessionAsync(OperationToken token, string address)
    {
        return A2dp.StartAudioSessionAsync(token, address);
    }

    // Object Push (OPP) based operations
    public Task<ErrorData> StartFileTransferSessionAsync(OperationToken token, string address)
    {
        return Opp.StartFileTransferSessionAsync(token, address);
    }

    public (FileTransferModel, ErrorData) QueueFileSend(
        OperationToken token,
        string address,
        string filepath
    )
    {
        return Opp.QueueFileSend(token, address, filepath);
    }

    public Task<ErrorData> SendFileAsync(OperationToken token, string address, string filepath)
    {
        return Opp.SendFileAsync(token, address, filepath);
    }

    public ErrorData CancelFileTransfer(OperationToken token, string address)
    {
        return Opp.CancelFileTransfer(token, address);
    }

    public ErrorData StopFileTransferSession(OperationToken token, string address)
    {
        return Opp.StopFileTransferSession(token, address);
    }

    #endregion
}
