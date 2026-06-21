using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

[DebuggerDisplay("Call({FunctionName})")]
public sealed class FunctionCall : OperandBase
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
