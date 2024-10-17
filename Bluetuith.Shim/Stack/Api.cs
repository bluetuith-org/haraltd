using Bluetuith.Shim.Executor;
using Bluetuith.Shim.Stack.Models;
using Bluetuith.Shim.Stack.Providers.MSFT;
using Bluetuith.Shim.Types;

namespace Bluetuith.Shim.Stack;

public interface IStack
{
    #region Adapter Methods
    public Task<(AdapterModel, ErrorData)> GetAdapterAsync();
    public Task<ErrorData> SetAdapterStateAsync(bool enable);
    public Task<ErrorData> StartDeviceDiscoveryAsync(OperationToken token, int timeout = 0);
    public Task<(GenericResult<List<DeviceModel>>, ErrorData)> GetPairedDevices();
    public Task<ErrorData> DisconnectDeviceAsync(string address);
    public Task<ErrorData> RemoveDeviceAsync(string address);
    #endregion

    #region Device Methods
    public Task<(DeviceModel, ErrorData)> GetDeviceAsync(OperationToken token, string address, bool inquireIfNotFound, bool appendServices);

    public Task<ErrorData> PairAsync(OperationToken token, string address, int timeout = 10);

    // Advanced Audio Distribution (A2DP) based operations
    public Task<ErrorData> StartAudioSessionAsync(OperationToken token, string address);

    // Message Access (MAP) based operations
    public Task<ErrorData> StartNotificationEventServerAsync(OperationToken token, string address);
    public Task<(MessageListingModel, ErrorData)> GetMessagesAsync(OperationToken token, string address, string folderPath = "telecom", ushort fromIndex = 0, ushort maxMessageCount = 0);
    public Task<(BMessageModel, ErrorData)> ShowMessageAsync(OperationToken token, string address, string handle);
    public Task<(GenericResult<int>, ErrorData)> GetTotalMessageCountAsync(OperationToken token, string address, string folderPath);
    public Task<(GenericResult<List<string>>, ErrorData)> GetMessageFoldersAsync(OperationToken token, string address, string folderPath = "");

    // Phonebook Access profile (PBAP) based operations
    public Task<(VcardModel, ErrorData)> GetAllContactsAsync(OperationToken token, string address);
    public Task<(VcardModel, ErrorData)> GetCombinedCallHistoryAsync(OperationToken token, string address);
    public Task<(VcardModel, ErrorData)> GetIncomingCallsHistoryAsync(OperationToken token, string address);
    public Task<(VcardModel, ErrorData)> GetOutgoingCallsHistoryAsync(OperationToken token, string address);
    public Task<(VcardModel, ErrorData)> GetMissedCallsAsync(OperationToken token, string address);
    public Task<(VcardModel, ErrorData)> GetSpeedDialAsync(OperationToken token, string address);

    // Object Push profile (OPP) based operations
    public Task<ErrorData> StartFileTransferClientAsync(OperationToken token, string address, params string[] files);
    public Task<ErrorData> StartFileTransferServerAsync(OperationToken token, string destinationDirectory);
    #endregion
}

public static class AvailableStacks
{
    public static IStack[] Stacks { get; } = [new MSFTStack()];
    public static IStack CurrentStack { get; set; } = Stacks[0];
}