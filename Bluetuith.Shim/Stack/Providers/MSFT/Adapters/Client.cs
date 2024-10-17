using InTheHand.Net.Bluetooth;

namespace Bluetuith.Shim.Stack.Providers.MSFT.Adapters;

internal sealed class Client
{
    private static readonly BluetoothClient _client = new();
    public static BluetoothClient Handle
    {
        get
        {
            _client.Authenticate = true;

            return _client;
        }
    }

    public static void CloseHandle()
    {
        _client?.Close();
    }
}