using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack;

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Perform operations on a Bluetooth device.</summary>
[RegisterCommands("device")]
public class Device
{
    /// <summary>Get information about a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public int Properties(ConsoleAppContext context, string address)
    {
        var (device, error) = OperationHost.Instance.Stack.GetDevice(
            (OperationToken)context.State,
            address
        );

        return Output.Result(device, error, (OperationToken)context.State);
    }

    /// <summary>Disconnect from a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public int Disconnect(ConsoleAppContext context, string address)
    {
        var error = OperationHost.Instance.Stack.DisconnectDevice(address);

        return Output.Error(error, (OperationToken)context.State);
    }

    /// <summary>Remove a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Remove(ConsoleAppContext context, string address)
    {
        var error = await OperationHost.Instance.Stack.RemoveDeviceAsync(address);

        return Output.Error(error, (OperationToken)context.State);
    }
}

[RegisterCommands("device pair")]
public class Pairing
{
    /// <summary>Pair with a Bluetooth device.</summary>
    /// <param name="timeout">
    ///     -t, The maximum amount of time in seconds that a pairing request can wait for a reply during
    ///     authentication.
    /// </param>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Command("")]
    public async Task<int> Pair(ConsoleAppContext context, string address, int timeout = 10)
    {
        var error = await OperationHost.Instance.Stack.PairAsync(
            (OperationToken)context.State,
            address,
            timeout
        );

        return Output.Error(error, (OperationToken)context.State);
    }

    [Hidden]
    /// <summary>Cancel pairing with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public int Cancel(ConsoleAppContext context, string address)
    {
        var error = OperationHost.Instance.Stack.CancelPairing(address);

        return Output.Error(error, (OperationToken)context.State);
    }
}

[RegisterCommands("device connect")]
public class Connection
{
    /// <summary>
    ///     Connect to a Bluetooth device. If this command is used without any subcommands and an address parameter is
    ///     specified, it will try to connect to the specified device automatically.
    /// </summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Command("")]
    public int Connect(ConsoleAppContext context, string address)
    {
        var error = OperationHost.Instance.Stack.Connect((OperationToken)context.State, address);

        return Output.Error(error, (OperationToken)context.State);
    }

    /// <summary>Connect to a device using a specified profile"</summary>
    /// <param name="uuid">-u, The Bluetooth service profile as a UUID.</param>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public int Profile(ConsoleAppContext context, string address, Guid uuid)
    {
        var error = OperationHost.Instance.Stack.ConnectProfile(
            (OperationToken)context.State,
            address,
            uuid
        );

        return Output.Error(error, (OperationToken)context.State);
    }
}

[RegisterCommands("device a2dp")]
public class A2dp
{
    /// <summary>Start an A2DP session with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> StartSession(ConsoleAppContext context, string address)
    {
        var error = await OperationHost.Instance.Stack.StartAudioSessionAsync(
            (OperationToken)context.State,
            address
        );

        return Output.Error(error, (OperationToken)context.State);
    }
}

/// <summary>Perform a Object Push Profile based operation.</summary>
[RegisterCommands("device opp")]
public class Opp
{
    [Hidden]
    /// <summary>Start an Object Push client session with a device (RPC mode only).</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> StartSession(ConsoleAppContext context, string address)
    {
        var error = await OperationHost.Instance.Stack.StartFileTransferSessionAsync(
            (OperationToken)context.State,
            address
        );

        return Output.Error(error, (OperationToken)context.State);
    }

    [Hidden]
    /// <summary>Send a file to a device with an open session. (RPC mode only).</summary>
    /// <param name="file">-f, A full path of the file to send.</param>
    public int SendFile(ConsoleAppContext context, string address, string file)
    {
        var token = (OperationToken)context.State;
        var (filetransfer, error) = OperationHost.Instance.Stack.QueueFileSend(
            token,
            address,
            file
        );

        return Output.Result(filetransfer, error, token);
    }

    [Hidden]
    /// <summary>Cancel an Object Push file transfer with a device (RPC mode only).</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public int CancelTransfer(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State;

        var error = OperationHost.Instance.Stack.CancelFileTransfer(token, address);

        return Output.Error(error, token);
    }

    [Hidden]
    /// <summary>Suspend an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    public int SuspendTransfer(ConsoleAppContext context)
    {
        return Output.Error(Errors.ErrorUnsupported, (OperationToken)context.State);
    }

    [Hidden]
    /// <summary>Resume an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    public int ResumeTransfer(ConsoleAppContext context)
    {
        return Output.Error(Errors.ErrorUnsupported, (OperationToken)context.State);
    }

    [Hidden]
    /// <summary>Stop an Object Push client session with a device (RPC mode only).</summary>
    public int StopSession(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State;

        return Output.Error(
            OperationHost.Instance.Stack.StopFileTransferSession(token, address),
            token
        );
    }
}
