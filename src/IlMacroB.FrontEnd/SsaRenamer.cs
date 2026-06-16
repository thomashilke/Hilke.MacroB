namespace Rollomatic.IlMacroB.FrontEnd;

public static class SsaRenamer
{
    public static void Rename(IReadOnlyList<TacInstructionBlock> tacBlocks)
    {
        var entry = tacBlocks.Single(b => b.BasicBlock.IsInitial);

        var immediateDominator = DominanceEngine.ComputeImmediateDominator(tacBlocks, entry);
        var dominanceFrontiers = DominanceEngine.ComputeFrontiers(tacBlocks, entry);
        DominanceEngine.InsertPhiNodes(tacBlocks, dominanceFrontiers);
        new SsaRecursiveRenamer(entry, immediateDominator);
    }
}

public static class ConstantPropagator
{
    public static void PropagateConstants(IReadOnlyList<TacInstructionBlock> tacBlocks)
    {
        throw new NotImplementedException();
    }
}