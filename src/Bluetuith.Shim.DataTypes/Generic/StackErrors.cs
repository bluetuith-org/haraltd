namespace Bluetuith.Shim.DataTypes.Generic;

internal record StackErrorCode : ErrorCode
{
    internal static readonly ErrorCode ErrAdapterNotFound = new StackErrorCode(
        ErrorCodeValue.ErrAdapterNotFound
    );

    internal static readonly ErrorCode ErrAdapterPowerModeAccess = new StackErrorCode(
        ErrorCodeValue.ErrAdapterPowerModeAccess
    );

    internal static readonly ErrorCode ErrAdapterServicesNotSupported = new StackErrorCode(
        ErrorCodeValue.ErrAdapterServicesNotSupported
    );

    internal static readonly ErrorCode ErrDeviceDiscovery = new StackErrorCode(
        ErrorCodeValue.ErrDeviceDiscovery
    );

    internal static readonly ErrorCode ErrDevicePairing = new StackErrorCode(
        ErrorCodeValue.ErrPairingReset
    );

    internal static readonly ErrorCode ErrDeviceServicesNotFound = new StackErrorCode(
        ErrorCodeValue.ErrDeviceServicesNotFound
    );

    internal static readonly ErrorCode ErrDeviceServicesNotSupported = new StackErrorCode(
        ErrorCodeValue.ErrDeviceServicesNotSupported
    );

    internal static ErrorCode ErrDeviceNotPaired = new StackErrorCode(
        ErrorCodeValue.ErrDeviceNotPaired
    );

    internal static readonly ErrorCode ErrDeviceUnpairing = new StackErrorCode(
        ErrorCodeValue.ErrDeviceUnpairing
    );

    internal static readonly ErrorCode ErrDeviceNotFound = new StackErrorCode(
        ErrorCodeValue.ErrDeviceNotFound
    );

    internal static readonly ErrorCode ErrDeviceNotConnected = new StackErrorCode(
        ErrorCodeValue.ErrDeviceNotConnected
    );

    internal static readonly ErrorCode ErrDeviceAlreadyConnected = new StackErrorCode(
        ErrorCodeValue.ErrDeviceAlreadyConnected
    );

    internal static readonly ErrorCode ErrDeviceDisconnect = new StackErrorCode(
        ErrorCodeValue.ErrDeviceDisconnect
    );

    internal static readonly ErrorCode ErrDeviceFileTransferSession = new StackErrorCode(
        ErrorCodeValue.ErrDeviceFileTransferClient
    );

    internal static ErrorCode ErrDeviceFileTransferServer = new StackErrorCode(
        ErrorCodeValue.ErrDeviceFileTransferServer
    );

    internal static readonly ErrorCode ErrDevicePhonebookClient = new StackErrorCode(
        ErrorCodeValue.ErrDevicePhonebookClient
    );

    internal static readonly ErrorCode ErrDeviceMessageAccessClient = new StackErrorCode(
        ErrorCodeValue.ErrDeviceMessageAccessClient
    );

    internal static readonly ErrorCode ErrDeviceMessageAccessServer = new StackErrorCode(
        ErrorCodeValue.ErrDeviceMessageAccessServer
    );

    internal static readonly ErrorCode ErrDeviceA2DpClient = new StackErrorCode(
        ErrorCodeValue.ErrDeviceA2DpClient
    );

    internal StackErrorCode(ErrorCodeValue value)
        : base(value.ToString(), (int)value)
    {
    }

    internal enum ErrorCodeValue
    {
        ErrAdapterNotFound,
        ErrAdapterPowerModeAccess,
        ErrAdapterServicesNotSupported,
        ErrDeviceDiscovery,
        ErrPairingReset,
        ErrDeviceServicesNotFound,
        ErrDeviceServicesNotSupported,
        ErrDeviceNotPaired,
        ErrDeviceUnpairing,
        ErrDeviceNotFound,
        ErrDeviceNotConnected,
        ErrDeviceAlreadyConnected,
        ErrDeviceDisconnect,
        ErrDeviceFileTransferClient,
        ErrDeviceFileTransferServer,
        ErrDevicePhonebookClient,
        ErrDeviceMessageAccessClient,
        ErrDeviceMessageAccessServer,
        ErrDeviceA2DpClient
    }
}

public partial class Errors
{
    public static readonly ErrorData ErrorAdapterNotFound = new(
        StackErrorCode.ErrAdapterNotFound,
        "An adapter was not found"
    );

    public static readonly ErrorData ErrorAdapterStateAccess = new(
        StackErrorCode.ErrAdapterPowerModeAccess,
        "An error occurred while accessing adapter states"
    );

    public static readonly ErrorData ErrorAdapterServicesNotSupported = new(
        StackErrorCode.ErrAdapterServicesNotSupported,
        "These services is not supported by the adapter."
    );

    public static readonly ErrorData ErrorDeviceDiscovery = new(
        StackErrorCode.ErrDeviceDiscovery,
        "An unexpected error occurred during device discovery"
    );

    public static readonly ErrorData ErrorDeviceNotConnected = new(
        StackErrorCode.ErrDeviceNotConnected,
        "The device is not connected"
    );

    public static readonly ErrorData ErrorDeviceAlreadyConnected = new(
        StackErrorCode.ErrDeviceAlreadyConnected,
        "The device is already connected"
    );

    public static readonly ErrorData ErrorDeviceDisconnect = new(
        StackErrorCode.ErrDeviceDisconnect,
        "The device cannot be disconnected"
    );

    public static readonly ErrorData ErrorDevicePairing = new(
        StackErrorCode.ErrDevicePairing,
        "Cannot pair device"
    );

    public static readonly ErrorData ErrorDeviceNotFound = new(
        StackErrorCode.ErrDeviceNotFound,
        "Cannot find device with the specified address"
    );

    public static readonly ErrorData ErrorDeviceUnpairing = new(
        StackErrorCode.ErrDeviceUnpairing,
        "The device cannot be unpaired"
    );

    public static readonly ErrorData ErrorDeviceServicesNotSupported = new(
        StackErrorCode.ErrDeviceServicesNotSupported,
        "These services is not supported by the device."
    );

    public static readonly ErrorData ErrorDeviceServicesNotFound = new(
        StackErrorCode.ErrDeviceServicesNotFound,
        "The device's list of services was not found"
    );

    public static readonly ErrorData ErrorDeviceFileTransferSession = new(
        StackErrorCode.ErrDeviceFileTransferSession,
        "An error occurred during a file transfer client session"
    );

    public static readonly ErrorData ErrorDevicePhonebookClient = new(
        StackErrorCode.ErrDevicePhonebookClient,
        "An error occurred during a phonebook download session"
    );

    public static readonly ErrorData ErrorDeviceMessageAccessClient = new(
        StackErrorCode.ErrDeviceMessageAccessClient,
        "An error occurred during a message access client session"
    );

    public static readonly ErrorData ErrorDeviceMessageAccessServer = new(
        StackErrorCode.ErrDeviceMessageAccessServer,
        "An error occurred during a message access server session"
    );

    public static readonly ErrorData ErrorDeviceA2dpClient = new(
        StackErrorCode.ErrDeviceA2DpClient,
        "An error occurred during an A2DP client session"
    );
}