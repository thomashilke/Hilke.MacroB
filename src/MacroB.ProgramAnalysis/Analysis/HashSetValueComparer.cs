namespace Hilke.MacroB.FrontEnd;

public sealed class HashSetValueComparer<TValue> : IEqualityComparer<HashSet<TValue>>
{
    public bool Equals(HashSet<TValue>? x, HashSet<TValue>? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.SetEquals(y);
    }

    public int GetHashCode(HashSet<TValue> obj)
    {
        return obj.Aggregate(0, (accumulate, next) => HashCode.Combine(accumulate, next.GetHashCode()));
    }
}