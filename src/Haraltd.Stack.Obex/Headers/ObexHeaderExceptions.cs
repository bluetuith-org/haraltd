namespace Haraltd.Stack.Obex.Headers;

public class ObexHeaderNotFoundException(HeaderId headerId)
    : Exception($"Can not find such header, HeaderId: {headerId} ")
{
    public HeaderId HeaderId { get; } = headerId;
}

public class ObexAppParameterNotFoundException(byte tagId)
    : ObexException($"Can not find such app parameter, TagId: {tagId}")
{
    public byte TagId { get; } = tagId;
}
