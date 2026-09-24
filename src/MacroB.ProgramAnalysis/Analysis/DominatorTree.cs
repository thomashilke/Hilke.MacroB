using System.Diagnostics;

namespace Hilke.MacroB.FrontEnd;

public class DominatorTree : IGraph<TacInstructionBlock>
{
    private readonly IDictionary<TacInstructionBlock, TacInstructionBlock> _immediateDominator;
    private readonly IDictionary<TacInstructionBlock, List<TacInstructionBlock>> _dominatorTree;

    public DominatorTree(IDictionary<TacInstructionBlock, TacInstructionBlock> immediateDominator)
    {
        _immediateDominator = immediateDominator.Where(kvp => kvp.Key != kvp.Value).ToDictionary();
        _dominatorTree = new Dictionary<TacInstructionBlock, List<TacInstructionBlock>>();

        foreach (var (dominated, dominator) in immediateDominator)
        {
            if (dominated == dominator)
            {
                continue;
            }

            if (!_dominatorTree.ContainsKey(dominator))
            {
                _dominatorTree.Add(dominator, new List<TacInstructionBlock>());
            }

            _dominatorTree[dominator].Add(dominated);
        }

        // Root must be resolved over every block the immediate-dominator map was computed for
        // (immediateDominator.Keys), not just _dominatorTree.Keys: a CFG with a single basic block
        // (no branches) has an empty dominator tree (the entry has no dominated children), so
        // _dominatorTree.Keys would be empty and Single() would throw even though the entry block
        // is unambiguously the root.
        Root = immediateDominator.Keys.Single(block => !_immediateDominator.ContainsKey(block));

        Debug.Assert(Root.IsEntry);
    }

    public TacInstructionBlock Root { get; }

    /// <inheritdoc />
    public IEnumerable<(TacInstructionBlock, TacInstructionBlock)> Edges =>
        _immediateDominator.Select(kvp => (kvp.Value, kvp.Key));

    /// <inheritdoc />
    IEnumerable<TacInstructionBlock> IGraph<TacInstructionBlock>.Vertices => _immediateDominator.Keys;

    /// <inheritdoc />
    IEnumerable<TacInstructionBlock> IGraph<TacInstructionBlock>.GetSuccessors(TacInstructionBlock vertex)
    {
        return _dominatorTree.TryGetValue(vertex, out var children)
                   ? children
                   : Enumerable.Empty<TacInstructionBlock>();
    }

    /// <inheritdoc />
    IEnumerable<TacInstructionBlock> IGraph<TacInstructionBlock>.GetPredecessors(TacInstructionBlock vertex)
    {
        return _immediateDominator.TryGetValue(vertex, out var dominator)
                   ? new[] { dominator }
                   : Enumerable.Empty<TacInstructionBlock>();
    }
}