using Bluetuith.Shim.Types;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bluetuith.Shim.Stack.Events;

public record class PairingAuthenticationEvent : AuthenticationEvent
{
    public PairingAuthenticationEvent(string pin, int timeout, CancellationTokenSource token) :
        base(pin, timeout, AuthenticationKind.ConfirmYesNo, token)
    { }

    public override string ToConsoleString()
    {
        return $"Confirm pairing with pin: {TextToValidate} (y/n)";
    }

    public override JsonObject ToJsonObject()
    {
        return new JsonObject()
        {
            ["pairingAuthenticationEvent"] = JsonSerializer.SerializeToNode(
                new JsonObject()
                {
                    ["pin"] = TextToValidate,
                    ["timeout"] = Timeout,
                    ["authenticationType"] = Kind.ToString()
                }
            )
        };
    }
}
