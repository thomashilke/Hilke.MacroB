using Hilke.DataStructures;

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

        public Dictionary<TVertex, int> GreedyColoring()
        {
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

public class BasicBlockOrderAnalysis
{
    private BasicBlockOrderAnalysis(List<TacInstructionBlock> blockOrder)
    {
        BlockOrder = blockOrder;
    }

    public List<TacInstructionBlock> BlockOrder { get; }

    public static BasicBlockOrderAnalysis Analyse(
        ControlFlowGraph<TacInstructionBlock> controlFlowGraph,
        ICostModel<TacInstructionBlock> transitionCostModel)
    {
        // Implementation of the Pettis-Hansen Greedy Algorithm
        var orderedEdges =
            (controlFlowGraph as IGraph<TacInstructionBlock>).Edges.OrderByDescending(edge => transitionCostModel
                .GetTransitionCost(
                    edge.Item1,
                    edge.Item2));
        var subpaths = new UnionFind<TacInstructionBlock>(controlFlowGraph.Blocks);
        var chains = controlFlowGraph.Blocks.ToDictionary(
            block => block,
            block => new List<TacInstructionBlock> { block });

        foreach (var edge in orderedEdges)
        {
            var sourceChain = chains[subpaths.Find(edge.Item1)];
            var targetChain = chains[subpaths.Find(edge.Item2)];

            if (edge.Item1 == sourceChain.Last() && edge.Item2 == targetChain.First())
            {
                subpaths.Union(edge.Item1, edge.Item2);
                var newRepresentant = subpaths.Find(edge.Item1);
                chains.Remove(edge.Item1);
                chains.Remove(edge.Item2);
                chains[newRepresentant] = sourceChain.Concat(targetChain).ToList();
            }
        }

        var blockOrder = new List<TacInstructionBlock>();
        foreach (var representant in subpaths.GetRepresentants())
        {
            blockOrder.AddRange(chains[representant]);
        }

        return new BasicBlockOrderAnalysis(blockOrder);
    }
}

public interface ICostModel<in TVertex>
{
    // This is a very naive naive class of cost model.
    // In practice, the cost of jumping from source to target may depends:
    //  - the order of source and target is the program file (jumping forward may be cheaper than jumping backward)
    //  - the total size of the program
    //  - the distance between source and target in the program
    //  - the recent execution trace, due to caching of recently seen jump target, or recently target jumped to,
    //  - the actual configuration of the CNC.
    // The list is not exhaustive
    double GetTransitionCost(TVertex source, TVertex target);
}