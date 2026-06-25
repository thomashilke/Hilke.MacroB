namespace Hilke.MacroB.FrontEnd;

public class UndirectedGraph<TVertex> : IGraph<TVertex>
{
    private readonly Dictionary<TVertex, HashSet<TVertex>> _adjacencyLists;

    private readonly HashSet<(TVertex, TVertex)> _edges;

    public UndirectedGraph(IEnumerable<TVertex> vertices, IEqualityComparer<TVertex>? comparer = null)
    {
        Comparer = comparer ?? EqualityComparer<TVertex>.Default;
        _adjacencyLists = vertices?.ToDictionary(v => v, _ => new HashSet<TVertex>(comparer), Comparer);
        _edges = new HashSet<(TVertex, TVertex)>();
    }

    public IEqualityComparer<TVertex> Comparer { get; }

    public IEnumerable<TVertex> Vertices => _adjacencyLists.Keys;

    public IEnumerable<(TVertex, TVertex)> Edges => _edges;

    /// <inheritdoc />
    public IEnumerable<TVertex> GetSuccessors(TVertex vertex)
    {
        return _adjacencyLists.TryGetValue(vertex, out var adjacencyList) ? adjacencyList : Enumerable.Empty<TVertex>();
    }

    /// <inheritdoc />
    public IEnumerable<TVertex> GetPredecessors(TVertex vertex)
    {
        return _adjacencyLists.TryGetValue(vertex, out var adjacencyList) ? adjacencyList : Enumerable.Empty<TVertex>();
    }

    public bool AddEdge(TVertex v1, TVertex v2)
    {
        var didAdd = false;
        {
            if (_adjacencyLists.TryGetValue(v1, out var adjacencyList))
            {
                didAdd |= adjacencyList.Add(v2);
            }
            else
            {
                _adjacencyLists[v1] = new HashSet<TVertex> { v2 };
                didAdd = true;
            }
        }

        {
            if (_adjacencyLists.TryGetValue(v2, out var adjacencyList))
            {
                didAdd |= adjacencyList.Add(v1);
            }
            else
            {
                _adjacencyLists[v2] = new HashSet<TVertex> { v1 };
                didAdd = true;
            }
        }

        if (didAdd)
        {
            _edges.Add((v1, v2));
        }

        return didAdd;
    }

    public bool RemoveEdge(TVertex v1, TVertex v2)
    {
        var didRemove = false;
        {
            if (_adjacencyLists.TryGetValue(v1, out var adjacencyList))
            {
                didRemove |= adjacencyList.Remove(v2);
            }
            else
            {
                return false;
            }
        }

        {
            if (_adjacencyLists.TryGetValue(v2, out var adjacencyList))
            {
                didRemove |= adjacencyList.Remove(v1);
            }
            else
            {
                return false;
            }
        }

        if (didRemove)
        {
            _edges.Remove((v1, v2));
            _edges.Remove((v2, v1));
        }

        return didRemove;
    }

    public bool AddVertex(TVertex v)
    {
        return _adjacencyLists.TryAdd(v, new HashSet<TVertex>(Comparer));
    }

    public bool RemoveVertex(TVertex v)
    {
        if (_adjacencyLists.TryGetValue(v, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                _adjacencyLists[neighbor].Remove(v);

                _edges.Remove((v, neighbor));
                _edges.Remove((neighbor, v));
            }

            _adjacencyLists.Remove(v);
            return true;
        }

        return false;
    }
}