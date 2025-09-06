using System.Diagnostics.CodeAnalysis;

namespace Haraltd.Stack.Obex.Utilities;

public class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    public static DictionaryEqualityComparer<TKey, TValue> Default { get; } = new();

    public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
    {
        if (x == y)
            return true;
        if (x == null || y == null)
            return false;
        return x.Count == y.Count && !x.Except(y).Any();
    }

    public int GetHashCode([DisallowNull] Dictionary<TKey, TValue> obj)
    {
        var hash = 13;
        var orderedKvpList = obj.OrderBy(kvp => kvp.Key);
        foreach (var kvp in orderedKvpList)
        {
            if (kvp.Key != null)
                hash = hash * 7 + kvp.Key.GetHashCode();
            if (kvp.Value != null)
                hash = hash * 7 + kvp.Value.GetHashCode();
        }

        return hash;
    }
}
