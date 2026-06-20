namespace Rollomatic.IlMacroB.FrontEnd;

public class LivenessRangeInterferenceAnalysis
{
    private LivenessRangeInterferenceAnalysis(UndirectedGraph<SsaVariable> livenessRangeInterferencesGraph)
    {
        LivenessRangeInterferencesGraph = livenessRangeInterferencesGraph
                                       ?? throw new ArgumentNullException(nameof(livenessRangeInterferencesGraph));
    }

    public UndirectedGraph<SsaVariable> LivenessRangeInterferencesGraph { get; }

    public static LivenessRangeInterferenceAnalysis Analyse(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var livenessAnalysis = LivenessAnalysis.Analyse(controlFlowGraph);
        var livenessRangeAnalysis = LivenessRangeAnalysis.Analyse(controlFlowGraph);

        var ranges = livenessRangeAnalysis.Ranges;
        var livenessRangeInterferencesGraph = new UndirectedGraph<SsaVariable>(ranges.GetRepresentants());

        // All the arguments version 0 must share distinct registers in the entry block.
        var argumentBaseNames = livenessRangeAnalysis.Variables.Select(variable => variable.Name).Distinct().ToList();
        for (var i = 0; i < argumentBaseNames.Count - 1; ++i)
        {
            for (var j = i + 1; j < argumentBaseNames.Count; ++j)
            {
                var arg1 = new SsaVariable(argumentBaseNames[i]);
                var arg2 = new SsaVariable(argumentBaseNames[j]);

                livenessRangeInterferencesGraph.AddEdge(
                    ranges.Find(arg1), 
                    ranges.Find(arg2));
            }
        }

        foreach (var block in controlFlowGraph.Blocks)
        {
            var liveNow = livenessAnalysis.LiveOut[block].ToHashSet();
            foreach (var instruction in block.Instructions.Reverse())
            {
                if (instruction.Destination is not null
                 && instruction.Op != Operand.Assign
                 && instruction.Op != Operand.Phi)
                {
                    var destRange = ranges.Find(instruction.Destination);
                    foreach (var liveRange in liveNow.Select(ranges.Find).Distinct())
                    {
                        livenessRangeInterferencesGraph.AddEdge(destRange, liveRange);
                    }

                    liveNow.Remove(instruction.Destination);
                    liveNow.UnionWith(instruction.Arguments.OfType<SsaVariable>());
                }
            }
        }

        return new LivenessRangeInterferenceAnalysis(livenessRangeInterferencesGraph);
    }
}

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