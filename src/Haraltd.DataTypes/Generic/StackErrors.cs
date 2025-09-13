namespace Haraltd.DataTypes.Generic;

internal record StackErrorCode : ErrorCode
{
    internal static ErrorCode ErrAdapterNotFound =>
        new StackErrorCode(ErrorCodeValue.ErrAdapterNotFound);

    internal static ErrorCode ErrAdapterPowerModeAccess =>
        new StackErrorCode(ErrorCodeValue.ErrAdapterPowerModeAccess);

    internal static ErrorCode ErrAdapterServicesNotSupported =>
        new StackErrorCode(ErrorCodeValue.ErrAdapterServicesNotSupported);

    internal static ErrorCode ErrDeviceDiscovery =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceDiscovery);

    internal static ErrorCode ErrDevicePairing =>
        new StackErrorCode(ErrorCodeValue.ErrPairingReset);

    internal static ErrorCode ErrDeviceServicesNotFound =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceServicesNotFound);

    internal static ErrorCode ErrDeviceServicesNotSupported =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceServicesNotSupported);

    internal static ErrorCode ErrDeviceNotPaired =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceNotPaired);

    internal static ErrorCode ErrDeviceAlreadyPaired =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceAlreadyPaired);

    internal static ErrorCode ErrDeviceUnpairing =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceUnpairing);

    internal static ErrorCode ErrDeviceNotFound =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceNotFound);

    internal static ErrorCode ErrDeviceNotConnected =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceNotConnected);

    internal static ErrorCode ErrDeviceAlreadyConnected =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceAlreadyConnected);

    internal static ErrorCode ErrDeviceDisconnect =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceDisconnect);

    internal static ErrorCode ErrDeviceFileTransferSession =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceFileTransferClient);

    internal static ErrorCode ErrDeviceFileTransferServer =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceFileTransferServer);

    internal static ErrorCode ErrDevicePhonebookClient =>
        new StackErrorCode(ErrorCodeValue.ErrDevicePhonebookClient);

    internal static ErrorCode ErrDeviceMessageAccessClient =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceMessageAccessClient);

    internal static ErrorCode ErrDeviceMessageAccessServer =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceMessageAccessServer);

    internal static ErrorCode ErrDeviceA2DpClient =>
        new StackErrorCode(ErrorCodeValue.ErrDeviceA2DpClient);

    private StackErrorCode(ErrorCodeValue value)
        : base(value.ToString(), (int)value) { }

    private enum ErrorCodeValue
    {
        ErrAdapterNotFound,
        ErrAdapterPowerModeAccess,
        ErrAdapterServicesNotSupported,
        ErrDeviceDiscovery,
        ErrPairingReset,
        ErrDeviceServicesNotFound,
        ErrDeviceServicesNotSupported,
        ErrDeviceNotPaired,
        ErrDeviceAlreadyPaired,
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
        ErrDeviceA2DpClient,
    }
}

public partial class Errors
{
    public static ErrorData ErrorAdapterNotFound =>
        new(StackErrorCode.ErrAdapterNotFound, "An adapter was not found");

    public static ErrorData ErrorAdapterStateAccess =>
        new(
            StackErrorCode.ErrAdapterPowerModeAccess,
            "An error occurred while accessing adapter states"
        );

    public static ErrorData ErrorAdapterServicesNotSupported =>
        new(
            StackErrorCode.ErrAdapterServicesNotSupported,
            "These services is not supported by the adapter."
        );

    public static ErrorData ErrorDeviceDiscovery =>
        new(
            StackErrorCode.ErrDeviceDiscovery,
            "An unexpected error occurred during device discovery"
        );

    public static ErrorData ErrorDeviceNotConnected =>
        new(StackErrorCode.ErrDeviceNotConnected, "The device is not connected");

    public static ErrorData ErrorDeviceAlreadyConnected =>
        new(StackErrorCode.ErrDeviceAlreadyConnected, "The device is already connected");

    public static ErrorData ErrorDeviceAlreadyPaired =>
        new(StackErrorCode.ErrDeviceAlreadyPaired, "The device is already paired");

    public static ErrorData ErrorDeviceNotPaired =>
        new(StackErrorCode.ErrDeviceNotPaired, "The device is not paired");

    public static ErrorData ErrorDeviceDisconnect =>
        new(StackErrorCode.ErrDeviceDisconnect, "The device cannot be disconnected");

    public static ErrorData ErrorDevicePairing =>
        new(StackErrorCode.ErrDevicePairing, "Cannot pair device");

    public static ErrorData ErrorDeviceNotFound =>
        new(StackErrorCode.ErrDeviceNotFound, "Cannot find device with the specified address");

    public static ErrorData ErrorDeviceUnpairing =>
        new(StackErrorCode.ErrDeviceUnpairing, "The device cannot be unpaired");

    public static ErrorData ErrorDeviceServicesNotSupported =>
        new(
            StackErrorCode.ErrDeviceServicesNotSupported,
            "These services is not supported by the device."
        );

    public static ErrorData ErrorDeviceServicesNotFound =>
        new(
            StackErrorCode.ErrDeviceServicesNotFound,
            "The device's list of services was not found"
        );

    public static ErrorData ErrorDeviceFileTransferSession =>
        new(
            StackErrorCode.ErrDeviceFileTransferSession,
            "An error occurred during a file transfer client session"
        );

    public static ErrorData ErrorDevicePhonebookClient =>
        new(
            StackErrorCode.ErrDevicePhonebookClient,
            "An error occurred during a phonebook download session"
        );

    public static ErrorData ErrorDeviceMessageAccessClient =>
        new(
            StackErrorCode.ErrDeviceMessageAccessClient,
            "An error occurred during a message access client session"
        );

    public static ErrorData ErrorDeviceMessageAccessServer =>
        new(
            StackErrorCode.ErrDeviceMessageAccessServer,
            "An error occurred during a message access server session"
        );

    public static ErrorData ErrorDeviceA2dpClient =>
        new(StackErrorCode.ErrDeviceA2DpClient, "An error occurred during an A2DP client session");
}
