namespace Hilke.MacroB.FrontEnd;

public interface IGraph<TVertex>
{
    IEnumerable<TVertex> Vertices { get; }

    IEnumerable<(TVertex, TVertex)> Edges { get; }

    IEnumerable<TVertex> GetSuccessors(TVertex vertex);

    IEnumerable<TVertex> GetPredecessors(TVertex vertex);
}

public interface IAdjacencyVertex<out TVertex> where TVertex : IAdjacencyVertex<TVertex>
{
    IReadOnlyList<TVertex> Successors { get; }

    IReadOnlyList<TVertex> Predecessors { get; }
}