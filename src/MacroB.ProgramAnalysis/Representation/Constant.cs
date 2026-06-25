using System.Diagnostics;

namespace Hilke.MacroB.FrontEnd;

[DebuggerDisplay("Const({Value})")]
public sealed class Constant : OperandBase
{
    public Constant(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return $"Const({Value})";
    }
}
