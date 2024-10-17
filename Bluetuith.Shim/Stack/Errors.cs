using Bluetuith.Shim.Types;

namespace Bluetuith.Shim.Stack;

public record class StackErrorCode : ErrorCode
{
    public enum ErrorCodeValue : int
    {
        ERR_ADAPTER_NOT_FOUND,
        ERR_ADAPTER_POWER_MODE_ACCESS,
        ERR_DEVICE_DISCOVERY,
        ERR_PAIRING_RESET,
        ERR_DEVICE_SERVICES_NOT_FOUND,
        ERR_DEVICE_NOT_PAIRED,
        ERR_DEVICE_UNPAIRING,
        ERR_DEVICE_NOT_FOUND,
        ERR_DEVICE_NOT_CONNECTED,
        ERR_DEVICE_DISCONNECT,
        ERR_DEVICE_FILE_TRANSFER_CLIENT,
        ERR_DEVICE_FILE_TRANSFER_SERVER,
        ERR_DEVICE_PHONEBOOK_CLIENT,
        ERR_DEVICE_MESSAGE_ACCESS_CLIENT,
        ERR_DEVICE_MESSAGE_ACCESS_SERVER,
        ERR_DEVICE_A2DP_CLIENT
    }

    public StackErrorCode(ErrorCodeValue value) : base(value.ToString(), (int)value) { }

    public static ErrorCode ERR_ADAPTER_NOT_FOUND = new StackErrorCode(ErrorCodeValue.ERR_ADAPTER_NOT_FOUND);
    public static ErrorCode ERR_ADAPTER_POWER_MODE_ACCESS = new StackErrorCode(ErrorCodeValue.ERR_ADAPTER_POWER_MODE_ACCESS);
    public static ErrorCode ERR_DEVICE_DISCOVERY = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_DISCOVERY);
    public static ErrorCode ERR_DEVICE_PAIRING = new StackErrorCode(ErrorCodeValue.ERR_PAIRING_RESET);
    public static ErrorCode ERR_DEVICE_SERVICES_NOT_FOUND = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_SERVICES_NOT_FOUND);
    public static ErrorCode ERR_DEVICE_NOT_PAIRED = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_NOT_PAIRED);
    public static ErrorCode ERR_DEVICE_UNPAIRING = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_UNPAIRING);
    public static ErrorCode ERR_DEVICE_NOT_FOUND = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_NOT_FOUND);
    public static ErrorCode ERR_DEVICE_NOT_CONNECTED = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_NOT_CONNECTED);
    public static ErrorCode ERR_DEVICE_DISCONNECT = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_DISCONNECT);
    public static ErrorCode ERR_DEVICE_FILE_TRANSFER_CLIENT = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_FILE_TRANSFER_CLIENT);
    public static ErrorCode ERR_DEVICE_FILE_TRANSFER_SERVER = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_FILE_TRANSFER_SERVER);
    public static ErrorCode ERR_DEVICE_PHONEBOOK_CLIENT = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_PHONEBOOK_CLIENT);
    public static ErrorCode ERR_DEVICE_MESSAGE_ACCESS_CLIENT = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_MESSAGE_ACCESS_CLIENT);
    public static ErrorCode ERR_DEVICE_MESSAGE_ACCESS_SERVER = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_MESSAGE_ACCESS_SERVER);
    public static ErrorCode ERR_DEVICE_A2DP_CLIENT = new StackErrorCode(ErrorCodeValue.ERR_DEVICE_A2DP_CLIENT);
}

public class StackErrors : Errors
{
    public static ErrorData ErrorAdapterNotFound = new(
        Code: StackErrorCode.ERR_ADAPTER_NOT_FOUND,
        Description: "An adapter was not found",
        Metadata: []
    );

    public static readonly ErrorData ErrorAdapterPowerModeAccess = new(
        Code: StackErrorCode.ERR_ADAPTER_POWER_MODE_ACCESS,
        Description: "An error occurred while accessing adapter power/scan states",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceDiscovery = new(
        Code: StackErrorCode.ERR_DEVICE_DISCOVERY,
        Description: "An unexpected error occurred during device discovery",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceNotConnected = new(
        Code: StackErrorCode.ERR_DEVICE_NOT_CONNECTED,
        Description: "The device is not connected",
        Metadata: []
    );

    public static readonly ErrorData ErrorDeviceDisconnect = new(
        Code: StackErrorCode.ERR_DEVICE_DISCONNECT,
        Description: "The device cannot be disconnected",
        Metadata: []
    );

    public static readonly ErrorData ErrorDevicePairing = new(
        Code: StackErrorCode.ERR_DEVICE_PAIRING,
        Description: "Cannot pair device",
        Metadata: new() {
            {"device-name", ""},
            {"device-address", ""},
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceNotFound = new(
        Code: StackErrorCode.ERR_DEVICE_NOT_FOUND,
        Description: "Cannot find device with the specified address",
        Metadata: new() {
            {"device-address", ""},
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceUnpairing = new(
        Code: StackErrorCode.ERR_DEVICE_UNPAIRING,
        Description: "The device cannot be unpaired",
        Metadata: new() {
            {"device-name", ""},
            {"device-address", ""},
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceServicesNotFound = new(
        Code: StackErrorCode.ERR_DEVICE_SERVICES_NOT_FOUND,
        Description: "The device's list of services was not found",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceFileTransferClient = new(
        Code: StackErrorCode.ERR_DEVICE_FILE_TRANSFER_CLIENT,
        Description: "An error occurred during a file transfer client session",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceFileTransferServer = new(
        Code: StackErrorCode.ERR_DEVICE_FILE_TRANSFER_SERVER,
        Description: "An error occurred during a file transfer server session",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDevicePhonebookClient = new(
        Code: StackErrorCode.ERR_DEVICE_PHONEBOOK_CLIENT,
        Description: "An error occurred during a phonebook download session",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceMessageAccessClient = new(
        Code: StackErrorCode.ERR_DEVICE_MESSAGE_ACCESS_CLIENT,
        Description: "An error occurred during a message access client session",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceMessageAccessServer = new(
        Code: StackErrorCode.ERR_DEVICE_MESSAGE_ACCESS_SERVER,
        Description: "An error occurred during a message access server session",
        Metadata: new() {
            {"exception", ""},
        }
    );

    public static readonly ErrorData ErrorDeviceA2dpClient = new(
        Code: StackErrorCode.ERR_DEVICE_A2DP_CLIENT,
        Description: "An error occurred during an A2DP client session",
        Metadata: new() {
            {"exception", ""},
        }
    );
}
