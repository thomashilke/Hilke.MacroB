namespace Rollomatic.IlMacroB.FrontEnd;

public class ControlFlowGraph : IGraph<BasicBlock>
{
    public ControlFlowGraph(IReadOnlyList<BasicBlock> blocks, BasicBlock initialBlock)
    {
        Blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        InitialBlock = initialBlock ?? throw new ArgumentNullException(nameof(initialBlock));
    }

    public IReadOnlyList<BasicBlock> Blocks { get; }

    public BasicBlock InitialBlock { get; }

    IEnumerable<BasicBlock> IGraph<BasicBlock>.GetPredecessors(BasicBlock vertex)
    {
        return vertex.Predecessors;
    }

    IEnumerable<BasicBlock> IGraph<BasicBlock>.GetSuccessors(BasicBlock vertex)
    {
        return vertex.Successors;
    }
}