using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Const({Value})")]
public sealed class Constant
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

[DebuggerDisplay("Call({FunctionName})")]
public sealed class FunctionCall
{
    public FunctionCall(string functionName, bool isPure)
    {
        FunctionName = functionName;
        IsPure = isPure;
    }

    public string FunctionName { get; }

    public bool IsPure { get; }

    public override string ToString()
    {
        return FunctionName;
    }
}