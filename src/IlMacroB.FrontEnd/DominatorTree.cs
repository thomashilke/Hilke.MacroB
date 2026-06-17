using System.Diagnostics;

namespace Rollomatic.IlMacroB.FrontEnd;

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

        Root = _dominatorTree.Keys.Single(block => !_immediateDominator.ContainsKey(block));

        Debug.Assert(Root.IsEntry);
    }

    public TacInstructionBlock Root { get; }

    /// <inheritdoc />
    IEnumerable<TacInstructionBlock> IGraph<TacInstructionBlock>.GetSuccessors(TacInstructionBlock vertex)
    {
        return _dominatorTree.TryGetValue(vertex, out var children) ? children : Enumerable.Empty<TacInstructionBlock>();
    }

    /// <inheritdoc />
    IEnumerable<TacInstructionBlock> IGraph<TacInstructionBlock>.GetPredecessors(TacInstructionBlock vertex)
    {
        return _immediateDominator.TryGetValue(vertex, out var dominator) ? new[] { dominator } : Enumerable.Empty<TacInstructionBlock>();
    }
}