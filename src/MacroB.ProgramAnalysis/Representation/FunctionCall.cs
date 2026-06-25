using System.Diagnostics;
using System.Reflection;

namespace Hilke.MacroB.FrontEnd;

[DebuggerDisplay("IntrinsicCall({FunctionName})")]
public sealed class IntrinsicFunctionCall : OperandBase
{
    public IntrinsicFunctionCall(string functionName, bool isPure)
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

[DebuggerDisplay("Call({FunctionName})")]
public sealed class FunctionCall : OperandBase
{
    public MethodInfo Method { get; }

    public FunctionCall(MethodInfo method)
    {
        Method = method;
    }

    public string FunctionName => Method.ToString();

    public override string ToString()
    {
        return Method.ToString();
    }
}
