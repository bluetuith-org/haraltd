using System.Text;
using System.Text.Json;
using Haraltd.DataTypes.Events;
using Haraltd.DataTypes.Generic;
using Haraltd.DataTypes.Serializer;

namespace Haraltd.DataTypes.Models;

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
