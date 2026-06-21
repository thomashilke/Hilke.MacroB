using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Block_at_{Target}")]
public class JumpTarget : OperandBase, IEquatable<JumpTarget>
{
    public JumpTarget(int target)
    {
        Target = target;
    }

    public int Target { get; }

    /// <inheritdoc />
    public bool Equals(JumpTarget? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Target == other.Target;
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

        return Equals((JumpTarget)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Target;
    }

    public static bool operator ==(JumpTarget? left, JumpTarget? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(JumpTarget? left, JumpTarget? right)
    {
        return !Equals(left, right);
    }
}
