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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        return Output.ResultWithContext(
            OperationHost.Instance.Stack.GetPlatformInfo(),
            Errors.ErrorNone,
            parserContext!.Token,
            parserContext
        );
    }

    /// <summary>Show the features of the RPC server.</summary>
    [Hidden]
    public int FeatureFlags(ConsoleAppContext context)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var features = OperationHost.Instance.Stack.GetFeatureFlags();

        return Output.ResultWithContext(
            features,
            Errors.ErrorNone,
            parserContext!.Token,
            parserContext
        );
    }

    /// <summary>Show the current version information of the RPC server.</summary>
    [Hidden]
    public int Version(ConsoleAppContext context)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        return Output.ResultWithContext(
            ConsoleApp.Version.ToResult("Version", "version"),
            Errors.ErrorNone,
            token,
            parserContext
        );
    }

    /// <summary>Set the response for a pending authentication request attached to an operation ID.</summary>
    /// <param name="response">-r, The response to sent to the authentication request.</param>
    /// <param name="authenticationId">-a, The ID of the authentication request.</param>
    [Hidden]
    public int Auth(ConsoleAppContext context, ushort authenticationId, string response)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var token = parserContext!.Token;

        if (!Output.ReplyToAuthenticationRequest(token, authenticationId, response))
            return Output.ErrorWithContext(
                Errors.ErrorOperationCancelled.AddMetadata(
                    "exception",
                    "No authentication ID or registered agent found: " + authenticationId
                ),
                token,
                parserContext
            );

        return Output.ErrorWithContext(Errors.ErrorNone, token, parserContext);
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
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var err = AuthenticationManager.RegisterAuthAgent(agentType, parserContext!.Token.ClientId);
        return Output.ErrorWithContext(err, parserContext!.Token, parserContext);
    }

    /// <summary>Unregister a registered agent for pairing or file-transfer related authentication events.</summary>
    /// <param name="agentType">-a, The type of agent that the client requests to be unregistered from.</param>
    [Hidden]
    public int Unregister(ConsoleAppContext context, AuthenticationManager.AuthAgentType agentType)
    {
        var parserContext = context.State as CommandParserContext;
        parserContext!.SetParsedAndWait();

        var err = AuthenticationManager.UnregisterAuthAgent(
            agentType,
            parserContext!.Token.ClientId
        );
        return Output.ErrorWithContext(err, parserContext!.Token, parserContext);
    }
}
