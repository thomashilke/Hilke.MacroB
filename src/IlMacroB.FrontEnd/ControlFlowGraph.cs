namespace Rollomatic.IlMacroB.FrontEnd;

public class ControlFlowGraph<TBlock> : IGraph<TBlock> where TBlock : IAdjacencyVertex<TBlock>
{
    public ControlFlowGraph(IReadOnlyList<TBlock> blocks, TBlock entryBlock)
    {
        Blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        EntryBlock = entryBlock ?? throw new ArgumentNullException(nameof(entryBlock));
    }

    public IReadOnlyList<TBlock> Blocks { get; }

    public TBlock EntryBlock { get; }

    /// <inheritdoc />
    public IEnumerable<(TBlock, TBlock)> Edges =>
        Blocks.SelectMany(block => block.Successors.Select(successor => (block, successor)));

    /// <inheritdoc />
    IEnumerable<TBlock> IGraph<TBlock>.Vertices => Blocks;

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