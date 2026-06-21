namespace Rollomatic.IlMacroB.FrontEnd;

public sealed class DataFlowAnalysisSolver<TState, TVertex>
    where TState : IEquatable<TState>
    where TVertex : notnull
{
    private readonly Func<TVertex, TState> _initializerDelegate;
    private readonly Func<TVertex, TState, TState> _transferDelegate;
    private readonly Func<IEnumerable<TState>, TState> _mergeDelegate;
    private readonly IEqualityComparer<TState> _equalityComparer;

    public DataFlowAnalysisSolver(
        Func<TVertex, TState> initializerDelegate,
        Func<TVertex, TState, TState> transferDelegate,
        Func<IEnumerable<TState>, TState> mergeDelegate,
        IEqualityComparer<TState>? equalityComparer = null)
    {
        _initializerDelegate = initializerDelegate;
        _transferDelegate = transferDelegate;
        _mergeDelegate = mergeDelegate;
        _equalityComparer = equalityComparer ?? EqualityComparer<TState>.Default;
    }

    public Dictionary<TVertex, TState> SolveBackwardFlow(IGraph<TVertex> graph)
    {
        var outStates = graph.Vertices.ToDictionary(vertex => vertex, vertex => _initializerDelegate(vertex));

        var workList = new Stack<TVertex>(graph.Vertices);
        while (workList.Any())
        {
            var currentVertex = workList.Pop();
            var inState = _mergeDelegate(graph.GetSuccessors(currentVertex)
                                              .Select(successor => outStates[successor]));
            var outState = _transferDelegate(currentVertex, inState);
            if (_equalityComparer.Equals(outStates[currentVertex], outState))
            {
                outStates[currentVertex] = outState;
                foreach (var predecessor in graph.GetPredecessors(currentVertex))
                {
                    workList.Push(predecessor);
                }
            }
        }

        return outStates;
    }

    public Dictionary<TVertex, TState> SolveForwardFlow(IGraph<TVertex> graph)
    {
        var inStates = graph.Vertices.ToDictionary(vertex => vertex, vertex => _initializerDelegate(vertex));

        var workList = new Stack<TVertex>(graph.Vertices);
        while (workList.Any())
        {
            var currentVertex = workList.Pop();
            var outState = _mergeDelegate(graph.GetPredecessors(currentVertex).Select(predecessor => inStates[predecessor]));
            var inState = _transferDelegate(currentVertex, outState);
            if (_equalityComparer.Equals(inStates[currentVertex], inState))
            {
                inStates[currentVertex] = inState;
                foreach (var successor in graph.GetSuccessors(currentVertex))
                {
                    workList.Push(successor);
                }
            }
        }

        return inStates;
    }
}
