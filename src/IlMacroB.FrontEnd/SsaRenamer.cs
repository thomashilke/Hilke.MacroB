namespace Rollomatic.IlMacroB.FrontEnd;

public class StaticSingleAssignmentRenameTransform : IControlFlowGraphTransformation<TacInstructionBlock>
{
    public void Transform(ControlFlowGraph<TacInstructionBlock> controlFlowGraph)
    {
        var entry = controlFlowGraph.EntryBlock;

        var immediateDominator = DominanceEngine.ComputeImmediateDominator(controlFlowGraph.Blocks, entry);
        var dominanceFrontiers = DominanceEngine.ComputeFrontiers(controlFlowGraph.Blocks, entry);
        DominanceEngine.InsertPhiNodes(controlFlowGraph.Blocks, dominanceFrontiers);

        var recursiveRenamer = new SingleStaticAssignmentRecursiveRenamer();
        recursiveRenamer.Rename(entry, immediateDominator);
    }
}