using Hilke.DataStructures;
using Hilke.MacroB.FrontEnd;

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
