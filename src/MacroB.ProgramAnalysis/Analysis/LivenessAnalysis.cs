namespace Hilke.MacroB.FrontEnd;

public class LivenessAnalysis
{
    public LivenessAnalysis(
        HashSet<SsaVariable> variables,
        Dictionary<TacInstructionBlock, HashSet<SsaVariable>> ueVar,
        Dictionary<TacInstructionBlock, HashSet<SsaVariable>> varNotKilled,
        Dictionary<TacInstructionBlock, HashSet<SsaVariable>> liveOut,
        Dictionary<TacInstructionBlock, HashSet<SsaVariable>> liveIn)
    {
        Variables = variables;
        UeVar = ueVar;
        VarNotKilled = varNotKilled;
        LiveOut = liveOut;
        LiveIn = liveIn;
    }

    public HashSet<SsaVariable> Variables { get; }

    public Dictionary<TacInstructionBlock, HashSet<SsaVariable>> UeVar { get; }

    public Dictionary<TacInstructionBlock, HashSet<SsaVariable>> VarNotKilled { get; }

    public Dictionary<TacInstructionBlock, HashSet<SsaVariable>> LiveOut { get; }

    public Dictionary<TacInstructionBlock, HashSet<SsaVariable>> LiveIn { get; }

    public static LivenessAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var variables = controlFlowGraph.GetVariables().ToHashSet();
        var blockInfo = controlFlowGraph.Blocks.ToDictionary(block => block, block => GetBlockInfo(block, variables));
        var stateComparer = new HashSetValueComparer<SsaVariable>();

        var solver = new DataFlowAnalysisSolver<HashSet<SsaVariable>, TacInstructionBlock>(
            initializerDelegate: EmptySet,
            transferDelegate: LiveIn,
            mergeDelegate: Union,
            stateComparer);

        var liveOut = solver.SolveBackwardFlow(controlFlowGraph);

        return new LivenessAnalysis(
            variables,
            blockInfo.ToDictionary(p => p.Key, p => p.Value.UeVar),
            blockInfo.ToDictionary(p => p.Key, p => p.Value.VarNotKilled),
            liveOut,
            liveOut.ToDictionary(p => p.Key, p => LiveIn(p.Key, p.Value)));

        // LiveIn variables of a block are either:
        //  - Upwards exposed variables (variables used before being defined in this block),
        //  - LiveOut variable that are not killed (defined) by this block.
        HashSet<SsaVariable> LiveIn(TacInstructionBlock block, HashSet<SsaVariable> liveOut)
        {
            return blockInfo[block].UeVar.Union(liveOut.Intersect(blockInfo[block].VarNotKilled)).ToHashSet();
        }

        HashSet<SsaVariable> Union(IEnumerable<HashSet<SsaVariable>> inStates)
        {
            return inStates.Aggregate(
                new HashSet<SsaVariable>(),
                (aggregate, next) =>
                {
                    aggregate.UnionWith(next);
                    return aggregate;
                });
        }

        HashSet<SsaVariable> EmptySet(TacInstructionBlock block)
        {
            return new HashSet<SsaVariable>();
        }
    }

    public static (HashSet<SsaVariable> UeVar, HashSet<SsaVariable> VarNotKilled) GetBlockInfo(
        TacInstructionBlock block,
        HashSet<SsaVariable> variables)
    {
        var ueVar = new HashSet<SsaVariable>(
            block.Phis.SelectMany(instruction => instruction.Arguments).OfType<SsaVariable>());
        var killVar = new HashSet<SsaVariable>(block.Phis.Select(instruction => instruction.Destination!));

        foreach (var instruction in block.Instructions.Where(instruction => instruction.Op != Operand.Phi))
        {
            ueVar.UnionWith(
                instruction.Arguments.SelectMany(argument => argument.GetReferencedVariables())
                                      .Where(variable => !killVar.Contains(variable)));
            if (instruction.Destination is { } destination)
            {
                killVar.Add(destination);
            }
        }

        return (ueVar, variables.Except(killVar).ToHashSet());
    }
}