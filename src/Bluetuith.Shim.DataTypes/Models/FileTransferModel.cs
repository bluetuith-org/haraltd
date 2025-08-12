using System.Text;
using System.Text.Json;
using Bluetuith.Shim.DataTypes.Events;
using Bluetuith.Shim.DataTypes.Generic;
using Bluetuith.Shim.DataTypes.Serializer;

namespace Bluetuith.Shim.DataTypes.Models;

public interface IFileTransfer : IFileTransferEvent;

public abstract record FileTransferBaseModel : FileTransferEventBaseModel, IFileTransfer;

public record FileTransferModel : FileTransferBaseModel, IResult
{
    public string ToConsoleString()
    {
        StringBuilder stringBuilder = new();

        AppendEventProperties(ref stringBuilder);

        return stringBuilder.ToString();
    }

    public void WriteJsonToStream(Utf8JsonWriter writer)
    {
        writer.WritePropertyName(SerializableContext.FileTransferPropertyName);
        (this as IFileTransfer).SerializeAll(writer);
    }
}