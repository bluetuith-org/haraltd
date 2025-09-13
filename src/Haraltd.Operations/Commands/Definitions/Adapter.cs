using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;

// ReSharper disable InvalidXmlDocComment

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Perform operations on the Bluetooth adapter.</summary>
[RegisterCommands("adapter")]
public class Adapter
{
    public enum ToggleState
    {
        On,
        Off,
    }

    /// <summary>List all available Bluetooth adapters.</summary>
    public int List(ConsoleAppContext context)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;
        var (adapters, error) = OperationHost.Instance.Stack.GetAdapters(token);

        return Output.ResultWithContext(
            adapters.ToResult("Adapters", "adapters"),
            error,
            token,
            parserContext
        );
    }

    /// <summary>Get information about the Bluetooth adapter.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> Properties(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;
        var props = new AdapterModel();

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            (props, error) = adapterResult.Adapter.Properties();

        return Output.ResultWithContext(props, error, token, parserContext);
    }

    /// <summary>Get the list of paired devices.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> GetPairedDevices(ConsoleAppContext context, string address)
    {
        GenericResult<List<DeviceModel>> devices = null;
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            (devices, error) = adapterResult.Adapter.GetPairedDevices();

        return Output.ResultWithContext(devices, error, token, parserContext);
    }

    /// <summary>Sets the power state of the adapter.</summary>
    /// <param name="state">-s, The adapter power state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> SetPoweredState(
        ConsoleAppContext context,
        ToggleState state,
        string address
    )
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            error = adapterResult.Adapter.SetPoweredState(state == ToggleState.On);

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Sets the pairable state of the adapter.</summary>
    /// <param name="state">-s, The adapter pairable state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> SetPairableState(
        ConsoleAppContext context,
        ToggleState state,
        string address
    )
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            error = adapterResult.Adapter.SetPairableState(state == ToggleState.On);

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Sets the discoverable state of the adapter.</summary>
    /// <param name="state">-s, The adapter discoverable state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> SetDiscoverableState(
        ConsoleAppContext context,
        ToggleState state,
        string address
    )
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            error = adapterResult.Adapter.SetDiscoverableState(state == ToggleState.On);

        return Output.ErrorWithContext(error, token, parserContext);
    }
}

/// <summary>Perform device discovery operations on the Bluetooth adapter.</summary>
[RegisterCommands("adapter discovery")]
public class Discovery
{
    /// <summary>Start a device discovery.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    /// <param name="timeout">-t, A value in seconds which determines when the device discovery will finish.</param>
    public async Task<int> Start(ConsoleAppContext context, string address, int timeout = 0)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            error = adapterResult.Adapter.StartDeviceDiscovery(timeout);

        return Output.ErrorWithContext(error, token, parserContext);
    }

    /// <summary>Stop a device discovery (RPC only).</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    [Hidden]
    public async Task<int> Stop(ConsoleAppContext context, string address)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        using var adapterResult = await OperationHost.Instance.Stack.GetAdapterAsync(
            address,
            token
        );
        var error = adapterResult.Error;
        if (adapterResult.Adapter != null)
            error = adapterResult.Adapter.StopDeviceDiscovery();

        return Output.ErrorWithContext(error, token, parserContext);
    }
}
