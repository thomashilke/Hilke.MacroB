namespace Rollomatic.IlMacroB.FrontEnd;

public class SsaVariable : OperandBase, IEquatable<SsaVariable>
{
    public SsaVariable(string name)
    {
        Name = name;
    }

    private SsaVariable(string name, int version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int Version { get; set; }

    /// <inheritdoc />
    public bool Equals(SsaVariable? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name
            && Version == other.Version;
    }

    public SsaVariable Clone()
    {
        return new SsaVariable(Name, Version);
    }

    public override string ToString()
    {
        return $"{Name}_{Version}";
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((SsaVariable)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Version);
    }

    public static bool operator ==(SsaVariable? left, SsaVariable? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SsaVariable? left, SsaVariable? right)
    {
        return !Equals(left, right);
    }
}
