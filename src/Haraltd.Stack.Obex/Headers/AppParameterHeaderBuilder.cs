using System.Buffers.Binary;

namespace Haraltd.Stack.Obex.Headers;

public class AppParameterHeaderBuilder
{
    public AppParameterHeaderBuilder(params AppParameter[] appParameters)
    {
        AppParameters = new List<AppParameter>();
        AppParameters.AddRange(appParameters);
    }

    public List<AppParameter> AppParameters { get; }

    public ObexHeader Build()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        foreach (var appParam in AppParameters)
        {
            writer.Write(appParam.TagId);
            writer.Write(BinaryPrimitives.ReverseEndianness(appParam.ContentLength));
            writer.Write(appParam.Buffer);
        }

        writer.Flush();
        return new ObexHeader(HeaderId.ApplicationParameters, memoryStream.ToArray());
    }
}
