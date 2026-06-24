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