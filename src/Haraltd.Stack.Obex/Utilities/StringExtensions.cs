using System.Text;

namespace Haraltd.Stack.Obex.Utilities;

public static class StringExtensions
{
    public static byte[] ToBytes(this string text, Encoding stringEncoding, bool nullTerminated)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        writer.Write(stringEncoding.GetBytes(text));
        if (nullTerminated)
            writer.Write(stringEncoding.GetBytes("\0"));
        writer.Flush();
        return memoryStream.ToArray();
    }
}
