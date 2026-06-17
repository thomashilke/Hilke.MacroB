namespace Rollomatic.IlMacroB.FrontEnd;

public static class SsaRenamer
{
    public static void Rename(IReadOnlyList<TacInstructionBlock> tacBlocks)
    {
        var entry = tacBlocks.Single(b => b.IsEntry);

        var immediateDominator = DominanceEngine.ComputeImmediateDominator(tacBlocks, entry);
        var dominanceFrontiers = DominanceEngine.ComputeFrontiers(tacBlocks, entry);
        DominanceEngine.InsertPhiNodes(tacBlocks, dominanceFrontiers);
        new SsaRecursiveRenamer(entry, immediateDominator);
    }
}