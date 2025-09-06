using Haraltd.Stack.Obex.Headers;

namespace Haraltd.Stack.Obex;

/// <summary>
///     Represent errors that occurred during the processing of OBEX packets.
/// </summary>
public class ObexException : Exception
{
    public ObexException() { }

    public ObexException(string message)
        : base(message) { }

    public ObexException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
///     The exception that is thrown when a OBEX request return an unsuccessful response (i.e. the remote service return an
///     unsuccessful opcode)
/// </summary>
public class ObexRequestException : ObexException
{
    public ObexRequestException(ObexOpcode opcode)
    {
        Opcode = opcode;
    }

    public ObexRequestException(ObexOpcode opcode, string message)
        : base(message)
    {
        Opcode = opcode;
    }

    public ObexRequestException(ObexOpcode opcode, string message, Exception innerException)
        : base(message, innerException)
    {
        Opcode = opcode;
    }

    /// <summary>
    ///     Represents a non-successful response opcode
    /// </summary>
    public ObexOpcode Opcode { get; }
}

public class InvalidObexOpcodeException : ObexException
{
    public InvalidObexOpcodeException(byte rawOpcode)
        : this(rawOpcode, $"Invalid opcode {rawOpcode}") { }

    public InvalidObexOpcodeException(byte rawOpcode, string message)
        : base(message)
    {
        RawOpcode = rawOpcode;
    }

    public InvalidObexOpcodeException(byte rawOpcode, string message, Exception innerException)
        : base(message, innerException)
    {
        RawOpcode = rawOpcode;
    }

    public byte RawOpcode { get; }
}
