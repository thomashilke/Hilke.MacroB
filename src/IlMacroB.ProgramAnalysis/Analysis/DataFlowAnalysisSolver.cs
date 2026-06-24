namespace Rollomatic.IlMacroB.FrontEnd;

public sealed class DataFlowAnalysisSolver<TState, TVertex>
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
        var inStates = graph.Vertices.ToDictionary(vertex => vertex, vertex => _initializerDelegate(vertex));
        var outStates = new Dictionary<TVertex, TState>();

        var workList = new Stack<TVertex>(graph.Vertices);
        while (workList.Any())
        {
            var currentVertex = workList.Pop();
            var outState = _mergeDelegate(graph.GetSuccessors(currentVertex)
                                              .Select(successor => inStates[successor]));
            var inState = _transferDelegate(currentVertex, outState);
            if (!_equalityComparer.Equals(inStates[currentVertex], inState))
            {
                inStates[currentVertex] = inState;
                foreach (var predecessor in graph.GetPredecessors(currentVertex))
                {
                    workList.Push(predecessor);
                }
            }
            outStates[currentVertex] = outState;
        }

        return outStates;
    }

    public Dictionary<TVertex, TState> SolveForwardFlow(IGraph<TVertex> graph)
    {
        var inStates = new Dictionary<TVertex, TState>();
        var outStates = graph.Vertices.ToDictionary(vertex => vertex, vertex => _initializerDelegate(vertex));

        var workList = new Stack<TVertex>(graph.Vertices);
        while (workList.Any())
        {
            var currentVertex = workList.Pop();
            var inState = _mergeDelegate(graph.GetPredecessors(currentVertex).Select(predecessor => outStates[predecessor]));
            var outState = _transferDelegate(currentVertex, inState);
            if (!_equalityComparer.Equals(outStates[currentVertex], outState))
            {
                outStates[currentVertex] = outState;
                foreach (var successor in graph.GetSuccessors(currentVertex))
                {
                    workList.Push(successor);
                }
            }
            inStates[currentVertex] = inState;
        }

        return inStates;
    }
}
