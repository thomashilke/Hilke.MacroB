using Hilke.DataStructures;

namespace Rollomatic.IlMacroB.FrontEnd;

public class LivenessRangeAnalysis
{
    private LivenessRangeAnalysis(UnionFind<SsaVariable> ranges)
    {
        Ranges = ranges;
    }

    public UnionFind<SsaVariable> Ranges { get; }

    public static LivenessRangeAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var ranges = new UnionFind<SsaVariable>();

        foreach (var variable in controlFlowGraph.GetVariables())
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

        return new LivenessRangeAnalysis(ranges);
    }

    public IEnumerable<IEnumerable<SsaVariable>> GetRanges()
    {
        return Ranges.GetElements().GroupBy(element => Ranges.Find(element));
    }
}