namespace Haraltd.Stack.Obex.Headers;

public partial class AppParameterDictionary : Dictionary<byte, AppParameter>
{
    public new AppParameter this[byte key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;

            throw new ObexAppParameterNotFoundException(key);
        }
        set => base[key] = value;
    }
}
