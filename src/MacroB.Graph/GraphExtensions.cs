using Hilke.DataStructures;

namespace Hilke.MacroB.FrontEnd;

public static class GraphExtensions
{
    extension<TVertex>(IGraph<TVertex> graph) where TVertex : notnull
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

        public Dictionary<TVertex, int> GreedyColoring()
        {
            // A liveness-range interference graph has zero vertices when the compiled unit needs no
            // general-purpose registers at all (e.g. a trivial `return <constant>;` method has no SSA
            // variables to color) — graph.Vertices.First() below would otherwise throw on that empty
            // graph even though "no vertices -> no colors" is the unambiguous correct answer.
            if (!graph.Vertices.Any())
            {
                return new Dictionary<TVertex, int>();
            }

            var coloring = new Dictionary<TVertex, int> { { graph.Vertices.First(), 0 } };

            var stack = new Stack<TVertex>(graph.Vertices);

            while (stack.Count() > 0)
            {
                var current = stack.Pop();

                if (coloring.ContainsKey(current))
                {
                    continue;
                }

                var distinctNeighborColors = graph.GetSuccessors(current)
                                                  .Where(neighbor => coloring.ContainsKey(neighbor))
                                                  .Select(neighbor => coloring[neighbor])
                                                  .Distinct()
                                                  .ToList();

                coloring[current] = distinctNeighborColors.Any() ? FirstFreeColor(distinctNeighborColors) : 0;

                static int FirstFreeColor(IEnumerable<int> colors)
                {
                    var candidateColor = 0;
                    while (colors.Contains(candidateColor))
                    {
                        candidateColor += 1;
                    }

                    return candidateColor;
                }

                foreach (var uncoloredNeighbors in graph.GetSuccessors(current)
                                                        .Where(neighbor => coloring.ContainsKey(neighbor)))
                {
                    stack.Push(uncoloredNeighbors);
                }
            }

            return coloring;
        }
    }
}
