using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            (props, error) = deviceResult.Device.Properties();

        return Output.ResultWithContext(props, error, token, parserContext);
    }

    /// <summary>Disconnect from a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Disconnect(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.DisconnectAsync();

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Remove a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Remove(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.RemoveAsync();

        return Output.ErrorWithContext(error, token, parserContext);
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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.PairAsync(timeout);

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Cancel pairing with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Hidden]
    public async Task<int> Cancel(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = deviceResult.Device.CancelPairing();

        return Output.ErrorWithContext(error, token, parserContext);
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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.ConnectAsync();

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Connect to a device using a specified profile</summary>
    /// <param name="uuid">-u, The Bluetooth service profile as a UUID.</param>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> Profile(ConsoleAppContext context, string address, Guid uuid)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.ConnectProfileAsync(uuid);

        return Output.ErrorWithContext(error, token, parserContext);
    }
}

[RegisterCommands("device a2dp")]
public class A2dp
{
    /// <summary>Start an A2DP session with a Bluetooth device.</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    public async Task<int> StartSession(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
            error = await deviceResult.Device.StartAudioSessionAsync();

        return Output.ErrorWithContext(error, token, parserContext);
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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
        {
            error = await deviceResult.Device.GetOppManager().StartFileTransferSessionAsync();
        }

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Send a file to a device with an open session. (RPC mode only).</summary>
    /// <param name="file">-f, A full path of the file to send.</param>
    [Hidden]
    public async Task<int> SendFile(ConsoleAppContext context, string address, string file)
    {
        var fileTransferModel = new FileTransferModel { Address = address, FileName = file };
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
        {
            (fileTransferModel, error) = deviceResult.Device.GetOppManager().QueueFileSend(file);
        }

        return Output.ResultWithContext(fileTransferModel, error, token, parserContext);
    }

    /// <summary>Cancel an Object Push file transfer with a device (RPC mode only).</summary>
    /// <param name="address">-a, The address of the Bluetooth device.</param>
    [Hidden]
    public async Task<int> CancelTransfer(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
        {
            error = await deviceResult.Device.GetOppManager().CancelFileTransferSessionAsync();
        }

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Suspend an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    [Hidden]
    public int SuspendTransfer(ConsoleAppContext context)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        return Output.ErrorWithContext(Errors.ErrorUnsupported, token, parserContext);
    }

    /// <summary>Resume an Object Push file transfer with a device (no-op, RPC mode only).</summary>
    [Hidden]
    public int ResumeTransfer(ConsoleAppContext context)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        return Output.ErrorWithContext(Errors.ErrorUnsupported, token, parserContext);
    }

    /// <summary>Stop an Object Push client session with a device (RPC mode only).</summary>
    [Hidden]
    public async Task<int> StopSession(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var deviceResult = await OperationHost.Instance.Stack.GetDeviceAsync(address, token);
        var error = deviceResult.Error;

        if (deviceResult.Device != null)
        {
            error = await deviceResult.Device.GetOppManager().StopFileTransferSessionAsync();
        }

        return Output.ErrorWithContext(error, token, parserContext);
    }
}
