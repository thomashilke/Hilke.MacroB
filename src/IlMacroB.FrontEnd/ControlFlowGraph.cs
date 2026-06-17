namespace Rollomatic.IlMacroB.FrontEnd;

public class ControlFlowGraph<TBlock> : IGraph<TBlock> where TBlock : IAdjacencyVertex<TBlock>
{
    public IReadOnlyList<TBlock> Blocks { get; }

    public TBlock EntryBlock { get; }

    public ControlFlowGraph(IReadOnlyList<TBlock> blocks, TBlock entryBlock)
    {
        Blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        EntryBlock = entryBlock ?? throw new ArgumentNullException(nameof(entryBlock));
    }

    /// <inheritdoc />
    IEnumerable<TBlock> IGraph<TBlock>.GetSuccessors(TBlock vertex)
    {
        return vertex.Successors;
    }

    /// <inheritdoc />
    IEnumerable<TBlock> IGraph<TBlock>.GetPredecessors(TBlock vertex)
    {
        return vertex.Predecessors;
    }
}