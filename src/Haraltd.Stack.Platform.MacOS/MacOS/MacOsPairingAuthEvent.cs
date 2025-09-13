using System.Text.Json;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.OperationToken;
using Haraltd.DataTypes.Serializer;
using InTheHand.Net;

namespace Haraltd.Stack.Platform.MacOS.MacOS;

public record class MacOsPairingAuthEvent : PairingAuthenticationEvent
{
    private readonly AuthenticationEventType _authEvent;

    public MacOsPairingAuthEvent(
        BluetoothAddress address,
        string pin,
        int timeout,
        AuthenticationEventType authenticationEvent,
        OperationToken token
    )
        : base(address, pin, timeout, GetReplyMethod(authenticationEvent), token)
    {
        _authEvent = authenticationEvent;
    }

    private static AuthenticationReplyMethod GetReplyMethod(
        AuthenticationEventType authenticationEvent
    )
    {
        return authenticationEvent switch
        {
            AuthenticationEventType.DisplayPasskey or AuthenticationEventType.DisplayPinCode =>
                AuthenticationReplyMethod.ReplyWithInput,
            _ => AuthenticationReplyMethod.ReplyYesNo,
        };
    }

    public override string ToConsoleString()
    {
        return _authEvent switch
        {
            AuthenticationEventType.DisplayPasskey or AuthenticationEventType.DisplayPinCode =>
                $"[{Address}] Enter pin/passkey on device: {TextToValidate}",
            _ =>
                $"[{Address}] Confirm pairing{(!string.IsNullOrEmpty(TextToValidate) ? " with passkey/pincode " + TextToValidate : "")}: ",
        };
    }

    public override void WriteJsonToStream(Utf8JsonWriter writer)
    {
        var parameters = new PairingParameters
        {
            AuthId = CurrentAuthId,
            Address = Address,
            AuthEvent = _authEvent,
            AuthReplyMethod = GetReplyMethod(_authEvent),
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
