namespace Rollomatic.IlMacroB.FrontEnd;

public static class GraphExtensions
{
    extension<TVertex>(IGraph<TVertex> graph)
    {
        public IEnumerable<TVertex> ReversePostOrder(TVertex root)
        {
            var visited = new HashSet<TVertex>();
            var postorder = new List<TVertex>();

            visit(root);

            postorder.Reverse();
            return postorder;

            void visit(TVertex vertex)
            {
                if (!visited.Add(vertex))
                {
                    return;
                }

                foreach (var successor in graph.GetSuccessors(vertex))
                {
                    visit(successor);
                }

                postorder.Add(vertex);
            }
        }

        public IEnumerable<TVertex> DepthFirstIterator(TVertex root)
        {
            var visited = new HashSet<TVertex>();
            var stack = new Stack<TVertex>();

            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!visited.Add(current))
                {
                    continue;
                }

                yield return current;

                foreach (var successor in graph.GetSuccessors(current))
                {
                    stack.Push(successor);
                }
            }
        }
    }
}