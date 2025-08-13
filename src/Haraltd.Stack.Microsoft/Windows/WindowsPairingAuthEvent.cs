using System.Text.Json;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;
using Haraltd.DataTypes.Serializer;
using Windows.Devices.Enumeration;
using SerializableContext = Haraltd.DataTypes.Serializer.SerializableContext;

namespace Haraltd.Stack.Microsoft.Windows;

internal record WindowsPairingAuthEvent : PairingAuthenticationEvent
{
    private readonly DevicePairingKinds _devicePairingKinds;

    internal WindowsPairingAuthEvent(
        string address,
        string pin,
        int timeout,
        DevicePairingKinds pairingKind,
        OperationToken token
    )
        : base(address, pin, timeout, GetReplyMethod(pairingKind), token)
    {
        _devicePairingKinds = pairingKind;
    }

    private static AuthenticationReplyMethod GetReplyMethod(DevicePairingKinds pairingKinds)
    {
        return pairingKinds switch
        {
            DevicePairingKinds.DisplayPin => AuthenticationReplyMethod.ReplyNone,
            DevicePairingKinds.ProvidePin => AuthenticationReplyMethod.ReplyNone,
            _ => AuthenticationReplyMethod.ReplyYesNo,
        };
    }

    private static AuthenticationEventType GetAuthEvent(DevicePairingKinds pairingKinds)
    {
        return pairingKinds switch
        {
            DevicePairingKinds.DisplayPin => AuthenticationEventType.DisplayPinCode,

            DevicePairingKinds.ConfirmOnly => AuthenticationEventType.AuthorizePairing,

            DevicePairingKinds.ConfirmPinMatch => AuthenticationEventType.ConfirmPasskey,
            _ => AuthenticationEventType.AuthEventNone,
        };
    }

    public override string ToConsoleString()
    {
        return _devicePairingKinds switch
        {
            DevicePairingKinds.DisplayPin => $"[{Address}] Enter pin on device: ({TextToValidate})",
            DevicePairingKinds.ConfirmPinMatch =>
                $"[{Address}] Confirm pairing with pin: {TextToValidate} (y/n)",
            _ => $"[{Address}] Confirm pairing (y/n)",
        };
    }

    public override void WriteJsonToStream(Utf8JsonWriter writer)
    {
        var parameters = new PairingParameters
        {
            AuthId = CurrentAuthId,
            Address = Address,
            AuthEvent = GetAuthEvent(_devicePairingKinds),
            AuthReplyMethod = GetReplyMethod(_devicePairingKinds),
            TimeoutMs = TimeoutMs,
        };

        if (!string.IsNullOrEmpty(TextToValidate))
            if (parameters.AuthEvent == AuthenticationEventType.DisplayPinCode)
                parameters.Pincode = TextToValidate;
            else
                try
                {
                    parameters.Passkey = Convert.ToUInt32(TextToValidate);
                }
                catch { }

        writer.WritePropertyName(SerializableContext.PairingAuthEventPropertyName);
        parameters.SerializeAll(writer);
    }
}
