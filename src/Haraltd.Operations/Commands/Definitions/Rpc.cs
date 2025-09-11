using ConsoleAppFramework;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.OperationToken;
using Haraltd.Operations.Managers;
using Haraltd.Operations.OutputStream;

// ReSharper disable PossibleNullReferenceException

namespace Haraltd.Operations.Commands.Definitions;

/// <summary>Perform functions related to an ongoing RPC operation within a session.</summary>
[Hidden]
[RegisterCommands("rpc")]
public class Rpc
{
    /// <summary>Get the platform-specific information of the Bluetooth stack and the Operating System the server is running on.</summary>
    [Hidden]
    public int PlatformInfo(ConsoleAppContext context)
    {
        return Output.Result(
            OperationHost.Instance.Stack.GetPlatformInfo(),
            Errors.ErrorNone,
            (OperationToken)context.State
        );
    }

    /// <summary>Show the features of the RPC server.</summary>
    [Hidden]
    public int FeatureFlags(ConsoleAppContext context)
    {
        var features = OperationHost.Instance.Stack.GetFeatureFlags();

        return Output.Result(features, Errors.ErrorNone, (OperationToken)context.State);
    }

    /// <summary>Show the current version information of the RPC server.</summary>
    [Hidden]
    public int Version(ConsoleAppContext context)
    {
        return Output.Result(
            ConsoleApp.Version.ToResult("Version", "version"),
            Errors.ErrorNone,
            (OperationToken)context.State
        );
    }

    /// <summary>Set the response for a pending authentication request attached to an operation ID.</summary>
    /// <param name="response">-r, The response to sent to the authentication request.</param>
    /// <param name="authenticationId">-a, The ID of the authentication request.</param>
    [Hidden]
    public int Auth(ConsoleAppContext context, ushort authenticationId, string response)
    {
        if (
            !Output.ReplyToAuthenticationRequest(
                (OperationToken)context.State,
                authenticationId,
                response
            )
        )
            return Output.Error(
                Errors.ErrorOperationCancelled.AddMetadata(
                    "exception",
                    "No authentication ID or registered agent found: " + authenticationId
                ),
                (OperationToken)context.State
            );

        return Output.Error(Errors.ErrorNone, (OperationToken)context.State);
    }
}

/// <summary>Set a primary agent for a client to receive authentication events.</summary>
[Hidden]
[RegisterCommands("rpc agent")]
public class Agent
{
    /// <summary>Register an agent for pairing or file-transfer related authentication events.</summary>
    /// <param name="agentType">-a, The type of agent that the client requests to be registered as.</param>
    [Hidden]
    public int Register(ConsoleAppContext context, AuthenticationManager.AuthAgentType agentType)
    {
        var err = AuthenticationManager.RegisterAuthAgent(
            agentType,
            ((OperationToken)context.State).ClientId
        );
        return Output.Error(err, (OperationToken)context.State);
    }

    /// <summary>Unregister a registered agent for pairing or file-transfer related authentication events.</summary>
    /// <param name="agentType">-a, The type of agent that the client requests to be unregistered from.</param>
    [Hidden]
    public int Unregister(ConsoleAppContext context, AuthenticationManager.AuthAgentType agentType)
    {
        var err = AuthenticationManager.UnregisterAuthAgent(
            agentType,
            ((OperationToken)context.State).ClientId
        );
        return Output.Error(err, (OperationToken)context.State);
    }
}
