using System.Reflection;

using Hilke.MacroB.FrontEnd;

namespace MacroB.Host.FrontEnd;

internal static class ProcedureDiscovery
{
    public static (IReadOnlyDictionary<MethodInfo, int> ProgramNumbers,
                   IReadOnlyDictionary<MethodInfo, MacroCallConvention> Conventions)
        Discover(MethodInfo entryMethod, IReadOnlyDictionary<MethodInfo, Operand> mathIntrinsics, MacroVariableConfiguration configuration)
    {
        var programNumbers = new Dictionary<MethodInfo, int>();
        var conventions = new Dictionary<MethodInfo, MacroCallConvention>();
        var visited = new HashSet<MethodInfo> { entryMethod };
        var queue = new Queue<MethodInfo>();
        queue.Enqueue(entryMethod);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var instruction in IlParser.ParseMethod(current))
            {
                if (instruction.OpCode.Name.StartsWith("call")
                 && instruction.Operand is MethodInfo called
                 && !mathIntrinsics.ContainsKey(called)
                 && !CncDeviceIntrinsics.Methods.Contains(called)
                 && visited.Add(called))
                {
                    // Throws NotSupportedException here if `called` is tagged Inline/MacroCallStyle2 —
                    // surfaced as soon as a rejected convention is reached, before any codegen.
                    conventions[called] = ProcedureConventions.Resolve(called);

                    if (programNumbers.Count >= 100)
                    {
                        throw new NotSupportedException(
                            $"{called} exceeds the 100-subprogram limit of this calling convention.");
                    }

                    programNumbers[called] = configuration.FirstProgramNumber + programNumbers.Count;
                    queue.Enqueue(called);
                }
            }
        }

        return (programNumbers, conventions);
    }
}
