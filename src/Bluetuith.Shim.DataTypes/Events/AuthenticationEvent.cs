using System.Text.Json;
using System.Text.Json.Serialization;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.Serializer;
using static Bluetuith.Shim.DataTypes.Generic.IEvent;

namespace Bluetuith.Shim.DataTypes.Events;

public record AuthenticationEvent : IEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter<AuthenticationEventType>))]
    public enum AuthenticationEventType : byte
    {
        AuthEventNone = 0,
        DisplayPinCode,
        DisplayPasskey,
        ConfirmPasskey,
        AuthorizePairing,
        AuthorizeService,
        AuthorizeTransfer
    }

    [JsonConverter(typeof(JsonStringEnumConverter<AuthenticationReplyMethod>))]
    public enum AuthenticationReplyMethod : byte
    {
        ReplyNone = 0,
        ReplyYesNo,
        ReplyWithInput
    }

    private static long _authIdNum;
    public readonly OperationToken.OperationToken Token;

    private bool _response;
    private bool _responseSet;

    public AuthenticationEvent(
        string textToValidate,
        int timeoutMs,
        AuthenticationReplyMethod replyMethod,
        OperationToken.OperationToken token
    )
    {
        ReplyMethod = replyMethod;
        TextToValidate = textToValidate;
        TimeoutMs = timeoutMs;
        Token = token;
    }

    public AuthenticationEvent(
        int timeout,
        AuthenticationReplyMethod replyMethod,
        OperationToken.OperationToken token
    )
        : this("", timeout, replyMethod, token)
    {
    }

    public long CurrentAuthId { get; } = ++_authIdNum;

    public AuthenticationReplyMethod ReplyMethod { get; }
    public string TextToValidate { get; }
    public int TimeoutMs { get; }

    EventType IEvent.Event => EventTypes.EventAuthentication;

    EventAction IEvent.Action
    {
        get => EventAction.Added;
        set => throw new InvalidDataException();
    }

    public virtual string ToConsoleString()
    {
        return "";
    }

    public virtual void WriteJsonToStream(Utf8JsonWriter writer)
    {
    }

    public bool WaitForResponse()
    {
        if (TimeoutMs > 0)
            if (!Token.ReleaseAfter(TimeoutMs))
                return false;

        if (ReplyMethod != AuthenticationReplyMethod.ReplyNone)
            Token.Wait();

        return !_responseSet ? SetResponse() : _response;
    }

    private bool SetResponse(string response = "")
    {
        _response = false;
        response = response.ToLower();

        switch (ReplyMethod)
        {
            case AuthenticationReplyMethod.ReplyYesNo:
                _response = response is "y" or "yes";
                break;

            case AuthenticationReplyMethod.ReplyWithInput:
                _response = response == TextToValidate;
                break;

            case AuthenticationReplyMethod.ReplyNone:
                _response = true;
                break;
        }

        _responseSet = true;
        Token.Release();

        return _response;
    }

    public bool TryAccept(string response)
    {
        return SetResponse(response);
    }

    public bool Deny()
    {
        return SetResponse();
    }

    public class AuthenticationParameters
    {
        [JsonPropertyName("auth_id")] public long AuthId { get; set; }

        [JsonPropertyName("auth_event")] public AuthenticationEventType AuthEvent { get; set; }

        [JsonPropertyName("auth_reply_method")]
        public AuthenticationReplyMethod AuthReplyMethod { get; set; }

        [JsonPropertyName("timeout_ms")] public int TimeoutMs { get; set; }
    }
}

public record PairingAuthenticationEvent : AuthenticationEvent
{
    protected readonly string Address;

    public PairingAuthenticationEvent(
        string address,
        string pin,
        int timeout,
        AuthenticationReplyMethod pairingKind,
        OperationToken.OperationToken token
    )
        : base(pin, timeout, pairingKind, token)
    {
        Address = address;
    }

    public class PairingParameters : AuthenticationParameters
    {
        [JsonPropertyName("address")] public string Address { get; set; }

        [JsonPropertyName("pincode")] public string Pincode { get; set; }

        [JsonPropertyName("passkey")] public uint Passkey { get; set; }
    }
}

public record OppAuthenticationEvent : AuthenticationEvent
{
    private readonly IFileTransferEvent _fileTransferEvent;

    public OppAuthenticationEvent(
        int timeout,
        IFileTransferEvent fileTransferEvent,
        OperationToken.OperationToken token
    )
        : base(timeout, AuthenticationReplyMethod.ReplyYesNo, token)
    {
        _fileTransferEvent = fileTransferEvent;
    }

    public override string ToConsoleString()
    {
        return $"Accept file {_fileTransferEvent.Name} from address {_fileTransferEvent.Address} (y/n)";
    }

    public override void WriteJsonToStream(Utf8JsonWriter writer)
    {
        var parameters = new OppParameters
        {
            AuthId = CurrentAuthId,
            AuthEvent = AuthenticationEventType.AuthorizeTransfer,
            AuthReplyMethod = AuthenticationReplyMethod.ReplyYesNo,
            TransferEvent = _fileTransferEvent,
            TimeoutMs = TimeoutMs
        };

        writer.WritePropertyName(SerializableContext.TransferAuthEventPropertyName);
        parameters.SerializeAll(writer);
    }

    public class OppParameters : AuthenticationParameters
    {
        [JsonPropertyName("file_transfer")] public IFileTransferEvent TransferEvent { get; set; }
    }
}