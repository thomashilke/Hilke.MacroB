namespace Rollomatic.IlMacroB.FrontEnd;

public static class GraphExtensions
{
    public static IEnumerable<TVertex> ReversePostOrder<TVertex>(this IGraph<TVertex> graph, TVertex root)
    {
        var visited = new HashSet<TVertex>();
        var postorder = new List<TVertex>();

        visit(root);

        postorder.Reverse();
        return postorder;

        void visit(TVertex vertex)
        {
            if (visited.Contains(vertex))
            {
                return;
            }
            visited.Add(vertex);

            foreach (var successor in graph.GetSuccessors(vertex))
            {
                visit(successor);
            }

            postorder.Add(vertex);
        }
    }
}
