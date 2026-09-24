using System.Reflection;

namespace Hilke.MacroB.FrontEnd;

public sealed class ProcedureCall : OperandBase
{
    public ProcedureCall(MethodInfo method, int programNumber, MacroCallConvention convention)
    {
        Method = method;
        ProgramNumber = programNumber;
        Convention = convention;
    }

    public MethodInfo Method { get; }

    public int ProgramNumber { get; }

    public MacroCallConvention Convention { get; }

    public override string ToString() => $"CallProcedure({Convention}, O{ProgramNumber})";
}

// FANUC Custom Macro B "Argument Specification I": letters A-Z except G,L,N,O,P (reserved for the G65
// statement itself) map to local variables #1-#26 with gaps at #10/#12/#14-16 — a fixed hardware fact,
// not part of MacroVariableConfiguration. Ordered ascending by target local-variable number so
// parameter 0 binds to the lowest-numbered local variable, parameter 1 the next, etc. (this compiler's
// own argument-position convention; FANUC does not mandate one). Duplicates
// Cnc.RuntimeEnvironment.ArgumentSpecificationI's facts because MacroB.BackEnd cannot reference
// MacroB.Host.FrontEnd.
public static class MacroCallStyle1Arguments
{
    public const int MaxArguments = 21;

    public static readonly IReadOnlyList<(char Letter, int LocalVariable)> Positions = new List<(char, int)>
    {
        ('A', 1), ('B', 2), ('C', 3), ('I', 4), ('J', 5), ('K', 6), ('D', 7), ('E', 8), ('F', 9),
        ('H', 11), ('M', 13), ('Q', 17), ('R', 18), ('S', 19), ('T', 20), ('U', 21), ('V', 22),
        ('W', 23), ('X', 24), ('Y', 25), ('Z', 26)
    };
}
