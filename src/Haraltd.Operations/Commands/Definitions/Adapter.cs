using ConsoleAppFramework;
using Haraltd.DataTypes.Models;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.OutputStream;
using Haraltd.Stack;

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
        var (adapter, error) = OperationHost.Instance.Stack.GetAdapter(
            (OperationToken)context.State
        );

        return Output.Result(
            new List<AdapterModel> { adapter }.ToResult("Adapters", "adapters"),
            error,
            (OperationToken)context.State
        );
    }

    /// <summary>Get information about the Bluetooth adapter.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int Properties(ConsoleAppContext context, string address)
    {
        var (adapter, error) = OperationHost.Instance.Stack.GetAdapter(
            (OperationToken)context.State
        );

        return Output.Result(adapter, error, (OperationToken)context.State);
    }

    /// <summary>Get the list of paired devices.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int GetPairedDevices(ConsoleAppContext context, string address)
    {
        var (devices, error) = OperationHost.Instance.Stack.GetPairedDevices();

        return Output.Result(devices, error, (OperationToken)context.State);
    }

    /// <summary>Sets the power state of the adapter.</summary>
    /// <param name="state">-s, The adapter power state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int SetPoweredState(ConsoleAppContext context, ToggleState state, string address)
    {
        var error = OperationHost.Instance.Stack.SetPoweredState(state == ToggleState.On);

        return Output.Error(error, (OperationToken)context.State);
    }

    /// <summary>Sets the pairable state of the adapter.</summary>
    /// <param name="state">-s, The adapter pairable state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int SetPairableState(ConsoleAppContext context, ToggleState state, string address)
    {
        var error = OperationHost.Instance.Stack.SetPairableState(state == ToggleState.On);

        return Output.Error(error, (OperationToken)context.State);
    }

    /// <summary>Sets the discoverable state of the adapter.</summary>
    /// <param name="state">-s, The adapter discoverable state to set.</param>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int SetDiscoverableState(ConsoleAppContext context, ToggleState state, string address)
    {
        var error = OperationHost.Instance.Stack.SetDiscoverableState(state == ToggleState.On);

        return Output.Error(error, (OperationToken)context.State);
    }
}

/// <summary>Perform device discovery operations on the Bluetooth adapter.</summary>
[RegisterCommands("adapter discovery")]
public class Discovery
{
    /// <summary>Start a device discovery.</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    /// <param name="timeout">-t, A value in seconds which determines when the device discovery will finish.</param>
    public int Start(ConsoleAppContext context, string address, int timeout = 0)
    {
        var error = OperationHost.Instance.Stack.StartDeviceDiscovery(
            (OperationToken)context.State,
            timeout
        );

        return Output.Error(error, (OperationToken)context.State);
    }

    [Hidden]
    /// <summary>Stop a device discovery (RPC only).</summary>
    /// <param name="address">-a, The address of the Bluetooth adapter.</param>
    public int Stop(ConsoleAppContext context, string address)
    {
        var error = OperationHost.Instance.Stack.StopDeviceDiscovery((OperationToken)context.State);

        return Output.Error(error, (OperationToken)context.State);
    }
}
