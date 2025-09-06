using System.Buffers.Binary;
using Haraltd.Stack.Obex.Streams;

namespace Haraltd.Stack.Obex.Headers;

public class AppParameterHeaderInterpreter : IBufferContentInterpreter<AppParameterDictionary>
{
    public AppParameterDictionary GetValue(ReadOnlySpan<byte> buffer)
    {
        AppParameterDictionary dict = new();
        for (var i = 0; i < buffer.Length; )
        {
            var tagId = buffer[i++];
            var len = BinaryPrimitives.ReverseEndianness(buffer[i++]);
            dict[tagId] = new AppParameter(tagId, buffer.Slice(i, len).ToArray());
            i += len;
        }

        return dict;
    }
}
