using Hilke.DataStructures;

namespace Rollomatic.IlMacroB.FrontEnd;


public class LivenessAnalysis
{
    public static LivenessAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var variables = controlFlowGraph.GetVariables().ToHashSet()
        var uevar = controlFlowGraph.Blocks.ToDictionary(block => block, block => block.GetUniqueVariableUses().ToHashSet());
        var varKill = controlFlowGraph.Blocks.ToDictionary(block => block, block => variables.Except(block.GetUniqueVariableDefinitions()).ToHashSet());

        var liveout = controlFlowGraph.Blocks.ToDictionary(block => block, _ => new HashSet<SsaVariable>());
    }
}

public class LivenessRangeAnalysis
{
    public UnionFind<SsaVariable> Ranges { get; }

    private LivenessRangeAnalysis(UnionFind<SsaVariable> ranges)
    {
        Ranges = ranges;
    }

    public static LivenessRangeAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var ranges = new UnionFind<SsaVariable>();

        foreach (var variable in controlFlowGraph.GetVariables()) 
        {
            ranges.Find(variable);
        }

        foreach (var instruction in controlFlowGraph.GetInstructions().Where(instruction => instruction.Op == Operand.Phi))
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