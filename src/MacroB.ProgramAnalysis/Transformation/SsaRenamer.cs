namespace Hilke.MacroB.FrontEnd;

public class StaticSingleAssignmentRenameTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    public ControlFlowGraph<TacInstructionBlock> Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var entry = controlFlowGraph.EntryBlock;

        var immediateDominator = DominanceEngine.ComputeImmediateDominator(controlFlowGraph.Blocks, entry);
        var dominanceFrontiers = DominanceEngine.ComputeFrontiers(controlFlowGraph.Blocks, entry);
        DominanceEngine.InsertPhiNodes(controlFlowGraph.Blocks, dominanceFrontiers);

        var recursiveRenamer = new SingleStaticAssignmentRecursiveRenamer();
        recursiveRenamer.Rename(entry, immediateDominator);

        return controlFlowGraph;
    }
}