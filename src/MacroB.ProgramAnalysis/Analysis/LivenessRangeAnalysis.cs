using Hilke.DataStructures;

namespace Hilke.MacroB.FrontEnd;

public class LivenessRangeAnalysis
{
    private LivenessRangeAnalysis(IEnumerable<SsaVariable> variables, UnionFind<SsaVariable> ranges)
    {
        Variables = variables.ToList();
        Ranges = ranges;
    }

    public IReadOnlyList<SsaVariable> Variables { get; }

    public UnionFind<SsaVariable> Ranges { get; }

    public static LivenessRangeAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var ranges = new UnionFind<SsaVariable>();
        var variables = controlFlowGraph.GetVariables();
        foreach (var variable in variables)
        {
            ranges.Find(variable);
        }

        foreach (var instruction in controlFlowGraph.GetInstructions()
                                                    .Where(instruction => instruction.Op == Operand.Phi))
        {
            foreach (var argument in instruction.Arguments)
            {
                ranges.Union(instruction.Destination, argument as SsaVariable);
            }
        }

        return new LivenessRangeAnalysis(variables, ranges);
    }

    public IEnumerable<(SsaVariable Representent, IEnumerable<SsaVariable> Set)> GetRanges()
    {
        return Ranges.GetElements()
                     .GroupBy(element => Ranges.Find(element))
                     .Select(g => (g.Key, g as IEnumerable<SsaVariable>));
    }
}