namespace Bluetuith.Shim.Types;

public record class EventCode
{
    public static EventCode None = new("None", 0);
    public static EventCode Authentication = new("Authentication", 1000);

    public string Name { get; }
    public int Value { get; }

    public EventCode(string name, int code)
    {
        Name = name;
        Value = code;
    }
}

public record class Event : Result
{
    public EventCode EventType = EventCode.None;
}

public record class AuthenticationEvent : Event
{
    public enum AuthenticationKind
    {
        ConfirmYesNo = 0,
        ConfirmInput
    }

    public AuthenticationKind Kind { get; }
    public string TextToValidate { get; }
    public int Timeout { get; }

    private readonly CancellationTokenSource _token;
    private bool _response = false;
    private bool _responseSet = false;

    public AuthenticationEvent(
        string textToValidate,
        int timeout,
        AuthenticationKind kind,
        CancellationTokenSource token
    )
    {
        EventType = EventCode.Authentication;
        Kind = kind;
        TextToValidate = textToValidate;
        Timeout = timeout;
        _token = token;
    }

    public AuthenticationEvent(int timeout, AuthenticationKind kind, CancellationTokenSource token) :
        this("", timeout, kind, token)
    { }

    public bool WaitForResponse()
    {
        _token.CancelAfter(Timeout);
        _token.Token.WaitHandle.WaitOne();

        return !_responseSet ? SetResponse() : _response;
    }

    public bool SetResponse(string response = "")
    {
        _response = false;
        response = response.ToLower();

        switch (Kind)
        {
            case AuthenticationKind.ConfirmYesNo:
                _response = response is "y" or "yes";
                break;

            case AuthenticationKind.ConfirmInput:
                _response = response == TextToValidate;
                break;
        }

        _responseSet = true;
        if (!_token.IsCancellationRequested)
        {
            _token.Cancel();
        }

        return _response;
    }
}