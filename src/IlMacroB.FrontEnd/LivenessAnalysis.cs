namespace Rollomatic.IlMacroB.FrontEnd;

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

        var liveOut = controlFlowGraph.Blocks.ToDictionary(block => block, _ => new HashSet<SsaVariable>());

        var didChange = true;

        while (didChange)
        {
            didChange = false;
            foreach (var block in controlFlowGraph.Blocks)
            {
                var newLiveOut = new HashSet<SsaVariable>();
                foreach (var successor in block.Successors)
                {
                    newLiveOut.UnionWith(LiveIn(successor));
                }

                if (!newLiveOut.SetEquals(liveOut[block]))
                {
                    didChange = true;
                    liveOut[block] = newLiveOut;
                }
            }
        }

        return new LivenessAnalysis(
            variables: variables,
            ueVar: blockInfo.ToDictionary(b => b.Key, b => b.Value.UeVar),
            varNotKilled: blockInfo.ToDictionary(b => b.Key, b => b.Value.VarNotKilled),
            liveOut: liveOut,
            liveIn: controlFlowGraph.Blocks.ToDictionary(block => block, block => LiveIn(block).ToHashSet()));

        IEnumerable<SsaVariable> LiveIn(TacInstructionBlock successor)
        {
            return blockInfo[successor].UeVar.Concat(liveOut[successor].Intersect(blockInfo[successor].VarNotKilled));
        }
    }

    public static (HashSet<SsaVariable> UeVar, HashSet<SsaVariable> VarNotKilled) GetBlockInfo(
        TacInstructionBlock block,
        HashSet<SsaVariable> variables)
    {
        var ueVar = new HashSet<SsaVariable>(
            block.Phis.SelectMany(instruction => instruction.Arguments).OfType<SsaVariable>());
        var killVar = new HashSet<SsaVariable>(block.Phis.Select(instruction => instruction.Destination));

        foreach (var instruction in block.Instructions.Where(instruction => instruction.Op != Operand.Phi))
        {
            ueVar.UnionWith(instruction.Arguments.OfType<SsaVariable>().Where(variable => !killVar.Contains(variable)));
            if (instruction.Destination is SsaVariable destination)
            {
                killVar.Add(destination);
            }
        }

        return (ueVar, variables.Except(killVar).ToHashSet());
    }
}