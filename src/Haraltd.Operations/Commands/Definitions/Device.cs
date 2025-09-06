using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;

// ReSharper disable InvalidXmlDocComment

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Perform operations on a Bluetooth device.</summary>
[RegisterCommands("device")]
public class Device
{
    /// <summary>Get information about a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Properties(ConsoleAppContext context, string address)
    {
        DeviceModel props = null;
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            (props, error) = device.Properties();

        return Output.Result(props, error, token);
    }

    /// <summary>Disconnect from a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Disconnect(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.DisconnectAsync();

        return Output.Error(error, token);
    }

    /// <summary>Remove a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Remove(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.RemoveAsync();

        return Output.Error(error, token);
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
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.PairAsync(timeout);

        return Output.Error(error, token);
    }

    /// <summary>Cancel pairing with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Hidden]
    public async Task<int> Cancel(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = device.CancelPairing();

        return Output.Error(error, token);
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
    public async Task<int> Connect(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.ConnectAsync();

        return Output.Error(error, token);
    }

    /// <summary>Connect to a device using a specified profile</summary>
    /// <param name="uuid">-u, The Bluetooth service profile as a UUID.</param>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Profile(ConsoleAppContext context, string address, Guid uuid)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.ConnectProfileAsync(uuid);

        return Output.Error(error, token);
    }
}

[RegisterCommands("device a2dp")]
public class A2dp
{
    /// <summary>Start an A2DP session with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> StartSession(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.StartAudioSessionAsync();

        return Output.Error(error, token);
    }
}

/// <summary>Perform an Object Push Profile based operation.</summary>
[RegisterCommands("device opp")]
public class Opp
{
    /// <summary>Start an Object Push client session with a device (RPC mode only).</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Hidden]
    public async Task<int> StartSession(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.StartFileTransferSessionAsync();

        return Output.Error(error, token);
    }

    /// <summary>Send a file to a device with an open session. (RPC mode only).</summary>
    /// <param name="file">-f, A full path of the file to send.</param>
    [Hidden]
    public async Task<int> SendFile(ConsoleAppContext context, string address, string file)
    {
        var fileTransferModel = new FileTransferModel();
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            (fileTransferModel, error) = device.QueueFileSend(file);

        return Output.Result(fileTransferModel, error, token);
    }

    /// <summary>Cancel an Object Push file transfer with a device (RPC mode only).</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Hidden]
    public async Task<int> CancelTransfer(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.CancelFileTransferSessionAsync();

        return Output.Error(error, token);
    }

    /// <summary>Suspend an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    [Hidden]
    public int SuspendTransfer(ConsoleAppContext context)
    {
        return Output.Error(Errors.ErrorUnsupported, (OperationToken)context.State!);
    }

    /// <summary>Resume an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    [Hidden]
    public int ResumeTransfer(ConsoleAppContext context)
    {
        return Output.Error(Errors.ErrorUnsupported, (OperationToken)context.State!);
    }

    /// <summary>Stop an Object Push client session with a device (RPC mode only).</summary>
    [Hidden]
    public async Task<int> StopSession(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (device, error) = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        if (device != null)
            error = await device.StopFileTransferSessionAsync();

        return Output.Error(error, token);
    }
}
