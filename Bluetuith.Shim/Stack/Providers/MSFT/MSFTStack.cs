using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Stack.Providers.MSFT.Adapters;
using Bluetuith.Shim.Stack.Providers.MSFT.Devices;
using Bluetuith.Shim.Stack.Providers.MSFT.Devices.Profiles;
using Bluetuith.Shim.Types;

namespace Bluetuith.Shim.Stack.Providers.MSFT;

public sealed class MSFTStack : IStack
{
    #region Adapter Methods
    public Task<(AdapterModel, ErrorData)> GetAdapterAsync() =>
        AdapterModelExt.ConvertToAdapterModelAsync();

    public Task<ErrorData> SetAdapterStateAsync(bool enable) =>
        AdapterMethods.SetAdapterStateAsync(enable);

    public Task<(GenericResult<List<DeviceModel>>, ErrorData)> GetPairedDevices() =>
        AdapterMethods.GetPairedDevices();

    public Task<ErrorData> StartDeviceDiscoveryAsync(OperationToken token, int timeout = 0) =>
        AdapterMethods.StartDeviceDiscoveryAsync(token, timeout);

    public Task<ErrorData> DisconnectDeviceAsync(string address) =>
        AdapterMethods.DisconnectDeviceAsync(address);

    public Task<ErrorData> RemoveDeviceAsync(string address) =>
        AdapterMethods.RemoveDeviceAsync(address);
    #endregion

    #region Device Methods
    public Task<(DeviceModel, ErrorData)> GetDeviceAsync(OperationToken token, string address, bool inquireIfNotFound, bool appendServices) =>
        DeviceModelExt.ConvertToDeviceModelAsync(address, inquireIfNotFound, appendServices, token.CancelToken);

    public Task<ErrorData> PairAsync(OperationToken token, string address, int timeout = 10) =>
        new Pairing(token).PairAsync(address, timeout);

    // Advanced Audio Distribution (A2DP) based operations
    public Task<ErrorData> StartAudioSessionAsync(OperationToken token, string address) =>
        A2dp.StartAudioSessionAsync(token, address);

    // Message Access (MAP) based operations
    public Task<ErrorData> StartNotificationEventServerAsync(OperationToken token, string address) =>
        Map.StartNotificationEventServerAsync(token, address);

    public Task<(MessageListingModel, ErrorData)> GetMessagesAsync(OperationToken token, string address, string folderPath = "telecom", ushort fromIndex = 0, ushort maxMessageCount = 0) =>
        Map.GetMessagesAsync(token, address, folderPath, fromIndex, maxMessageCount);

    public Task<(BMessageModel, ErrorData)> ShowMessageAsync(OperationToken token, string address, string handle) =>
        Map.ShowMessageAsync(token, address, handle);

    public Task<(GenericResult<int>, ErrorData)> GetTotalMessageCountAsync(OperationToken token, string address, string folderPath) =>
        Map.GetTotalMessageCountAsync(token, address, folderPath);

    public Task<(GenericResult<List<string>>, ErrorData)> GetMessageFoldersAsync(OperationToken token, string address, string folderPath = "") =>
        Map.GetMessageFoldersAsync(token, address, folderPath);

    // Phonebook Access PSE (PBAP) based operations
    public Task<(VcardModel, ErrorData)> GetAllContactsAsync(OperationToken token, string address) =>
        Pbap.GetAllContactsAsync(token, address);

    public Task<(VcardModel, ErrorData)> GetCombinedCallHistoryAsync(OperationToken token, string address) =>
        Pbap.GetCombinedCallHistoryAsync(token, address);

    public Task<(VcardModel, ErrorData)> GetIncomingCallsHistoryAsync(OperationToken token, string address) =>
        Pbap.GetIncomingCallsHistoryAsync(token, address);

    public Task<(VcardModel, ErrorData)> GetOutgoingCallsHistoryAsync(OperationToken token, string address) =>
        Pbap.GetOutgoingCallsHistoryAsync(token, address);

    public Task<(VcardModel, ErrorData)> GetMissedCallsAsync(OperationToken token, string address) =>
        Pbap.GetMissedCallsAsync(token, address);

    public Task<(VcardModel, ErrorData)> GetSpeedDialAsync(OperationToken token, string address) =>
        Pbap.GetSpeedDialAsync(token, address);

    // Object Push (OPP) based operations
    public Task<ErrorData> StartFileTransferClientAsync(OperationToken token, string address, params string[] files) =>
        Opp.StartFileTransferClientAsync(token, address, files);

    public Task<ErrorData> StartFileTransferServerAsync(OperationToken token, string destinationDirectory) =>
        Opp.StartFileTransferServerAsync(token, destinationDirectory);
    #endregion
}