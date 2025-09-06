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
        var token = (OperationToken)context.State!;
        var (adapters, error) = OperationHost.Instance.Stack.GetAdapters(token);

        return Output.Result(adapters.ToResult("Adapters", "adapters"), error, token);
    }

    /// <summary>Get information about the Bluetooth adapter.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> Properties(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;
        var props = new AdapterModel();

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            (props, error) = adapter.Properties();

        return Output.Result(props, error, token);
    }

    /// <summary>Get the list of paired devices.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public async Task<int> GetPairedDevices(ConsoleAppContext context, string address)
    {
        GenericResult<List<DeviceModel>> devices = null;
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            (devices, error) = adapter.GetPairedDevices();

        return Output.Result(devices, error, token);
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
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            error = adapter.SetPoweredState(state == ToggleState.On);

        return Output.Error(error, token);
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
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            error = adapter.SetPairableState(state == ToggleState.On);

        return Output.Error(error, token);
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
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            error = adapter.SetDiscoverableState(state == ToggleState.On);

        return Output.Error(error, token);
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
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            error = adapter.StartDeviceDiscovery(timeout);

        return Output.Error(error, token);
    }

    /// <summary>Stop a device discovery (RPC only).</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    [Hidden]
    public async Task<int> Stop(ConsoleAppContext context, string address)
    {
        var token = (OperationToken)context.State!;

        var (adapter, error) = await OperationHost.Instance.Stack.GetAdapterAsync(address, token);
        if (adapter != null)
            error = adapter.StopDeviceDiscovery();

        return Output.Error(error, token);
    }
}
